using BCrypt.Net;
using ERP.Auth.API.Dtos;
using ERP.Auth.Application.Services;
using ERP.Auth.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Shared.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Auth.Application.Commands;

public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IAuthDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IPermissionService _permissions;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<LoginHandler> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public LoginHandler(
        IAuthDbContext db,
        IJwtService jwtService,
        IPermissionService permissions,
        ICurrentTenant currentTenant,
        ILogger<LoginHandler> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _permissions = permissions;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (user is null)
            return Result<LoginResponse>.Failure("Invalid credentials.");

        if (user.IsLockedOut)
            return Result<LoginResponse>.Failure($"Account is locked. Try again after {user.LockoutEndAt:HH:mm} UTC.");

        if (!user.IsActive)
            return Result<LoginResponse>.Failure("Account is deactivated.");

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEndAt = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("User {UserId} locked out after {Attempts} failed attempts", user.Id, user.FailedLoginAttempts);
            }
            await _db.SaveChangesAsync(cancellationToken);
            return Result<LoginResponse>.Failure("Invalid credentials.");
        }

        // Reset failed attempts on success
        user.FailedLoginAttempts = 0;
        user.LockoutEndAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        // MFA required — return challenge token; client must call /api/auth/mfa/verify-login
        if (user.MfaEnabled)
        {
            var challengeToken = _jwtService.GenerateMfaChallengeToken(user.Id, user.TenantId);
            _logger.LogInformation("MFA challenge issued for user {UserId}", user.Id);
            return Result<LoginResponse>.Success(new LoginResponse(
                string.Empty, string.Empty, user.Id, user.Email, user.FullName,
                Array.Empty<string>(), MfaRequired: true, MfaChallengeToken: challengeToken));
        }

        var perms = await _permissions.GetUserPermissionsAsync(user.TenantId, user.Id, cancellationToken);
        var roles = perms.Roles;
        var permissions = perms.Permissions;

        var accessToken = _jwtService.GenerateAccessToken(user, roles, permissions);
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

        _logger.LogInformation("User {UserId} logged in (no MFA)", user.Id);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken,
            refreshTokenValue,
            user.Id,
            user.Email,
            user.FullName,
            roles
        ));
    }
}
