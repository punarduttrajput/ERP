using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ERP.Auth.API.Dtos;
using ERP.Auth.Application.Services;
using ERP.Auth.Domain;
using ERP.Shared.Application.Common;
using ERP.Shared.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Auth.Application.Commands;

public sealed class VerifyMfaLoginHandler : IRequestHandler<VerifyMfaLoginCommand, Result<LoginResponse>>
{
    private readonly IAuthDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly ITotpService _totp;
    private readonly IPermissionService _permissions;
    private readonly ILogger<VerifyMfaLoginHandler> _logger;

    public VerifyMfaLoginHandler(
        IAuthDbContext db, IJwtService jwtService, ITotpService totp,
        IPermissionService permissions, ILogger<VerifyMfaLoginHandler> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _totp = totp;
        _permissions = permissions;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(VerifyMfaLoginCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId, tenantId) = _jwtService.ValidateMfaChallengeToken(request.MfaChallengeToken);
        if (!isValid)
            return Result<LoginResponse>.Failure("Invalid or expired MFA challenge token.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("User not found or inactive.");

        if (!user.MfaEnabled || user.MfaSecret is null)
            return Result<LoginResponse>.Failure("MFA is not configured for this user.");

        var codeVerified = _totp.Verify(user.MfaSecret, request.Code)
                        || TryConsumeRecoveryCode(user, request.Code);

        if (!codeVerified)
            return Result<LoginResponse>.Failure("Invalid MFA code.");

        await _db.SaveChangesAsync(cancellationToken);

        var perms = await _permissions.GetUserPermissionsAsync(user.TenantId, user.Id, cancellationToken);
        var accessToken = _jwtService.GenerateAccessToken(user, perms.Roles, perms.Permissions);
        var (refreshTokenValue, familyId) = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = refreshTokenValue,
            FamilyId = familyId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress
        };

        await _db.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} completed MFA login", user.Id);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken, refreshTokenValue, user.Id, user.Email, user.FullName, perms.Roles));
    }

    private static bool TryConsumeRecoveryCode(Domain.User user, string code)
    {
        if (user.MfaRecoveryCodes is null) return false;

        var stored = JsonSerializer.Deserialize<List<string>>(user.MfaRecoveryCodes);
        if (stored is null) return false;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.ToLowerInvariant())));
        var index = stored.FindIndex(h => h.Equals(hash, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;

        stored.RemoveAt(index);
        user.MfaRecoveryCodes = JsonSerializer.Serialize(stored);
        return true;
    }
}
