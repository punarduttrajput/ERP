using ERP.Auth.Application.Services;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ERP.Auth.Application.Commands;

public sealed class EnableMfaHandler : IRequestHandler<EnableMfaCommand, Result<EnableMfaResponse>>
{
    private readonly IAuthDbContext _db;
    private readonly ITotpService _totp;
    private readonly ICacheService _cache;
    private readonly IConfiguration _config;

    public EnableMfaHandler(IAuthDbContext db, ITotpService totp, ICacheService cache, IConfiguration config)
    {
        _db = db;
        _totp = totp;
        _cache = cache;
        _config = config;
    }

    public async Task<Result<EnableMfaResponse>> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        if (user is null)
            return Result<EnableMfaResponse>.Failure("User not found.");

        if (user.MfaEnabled)
            return Result<EnableMfaResponse>.Failure("MFA is already enabled.");

        var secret = _totp.GenerateSecret();
        var issuer = _config["Jwt:Issuer"] ?? "ERP Platform";
        var qrUri  = _totp.GetQrCodeUri(secret, user.Email, issuer);

        // Store pending secret in cache (TTL 10 min) until the user confirms with a valid TOTP code
        await _cache.SetAsync($"mfa_pending:{user.Id}", secret, TimeSpan.FromMinutes(10), cancellationToken);

        return Result<EnableMfaResponse>.Success(new EnableMfaResponse(secret, qrUri));
    }
}
