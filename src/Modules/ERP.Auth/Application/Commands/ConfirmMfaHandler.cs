using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ERP.Auth.Application.Services;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Auth.Application.Commands;

public sealed class ConfirmMfaHandler : IRequestHandler<ConfirmMfaCommand, Result<ConfirmMfaResponse>>
{
    private readonly IAuthDbContext _db;
    private readonly ITotpService _totp;
    private readonly ICacheService _cache;

    public ConfirmMfaHandler(IAuthDbContext db, ITotpService totp, ICacheService cache)
    {
        _db = db;
        _totp = totp;
        _cache = cache;
    }

    public async Task<Result<ConfirmMfaResponse>> Handle(ConfirmMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        if (user is null)
            return Result<ConfirmMfaResponse>.Failure("User not found.");

        if (user.MfaEnabled)
            return Result<ConfirmMfaResponse>.Failure("MFA is already enabled.");

        var secret = await _cache.GetAsync<string>($"mfa_pending:{user.Id}", cancellationToken);
        if (secret is null)
            return Result<ConfirmMfaResponse>.Failure("MFA setup session expired. Please restart setup.");

        if (!_totp.Verify(secret, request.TotpCode))
            return Result<ConfirmMfaResponse>.Failure("Invalid TOTP code.");

        // Generate 8 one-time recovery codes
        var plainCodes = GenerateRecoveryCodes(8);
        var hashedCodes = plainCodes.Select(HashCode).ToArray();

        user.MfaEnabled = true;
        user.MfaSecret = secret;
        user.MfaRecoveryCodes = JsonSerializer.Serialize(hashedCodes);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync($"mfa_pending:{user.Id}", cancellationToken);

        return Result<ConfirmMfaResponse>.Success(new ConfirmMfaResponse(plainCodes));
    }

    private static IReadOnlyList<string> GenerateRecoveryCodes(int count)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);
            codes.Add(Convert.ToHexString(bytes).ToLowerInvariant());
        }
        return codes;
    }

    internal static string HashCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.ToLowerInvariant())));
}
