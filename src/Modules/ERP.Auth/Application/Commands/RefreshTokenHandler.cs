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

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IAuthDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IPermissionService _permissions;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        IAuthDbContext db,
        IJwtService jwtService,
        IPermissionService permissions,
        ILogger<RefreshTokenHandler> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _permissions = permissions;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.Token && !r.IsDeleted, cancellationToken);

        if (existingToken is null)
            return Result<LoginResponse>.Failure("Invalid refresh token.");

        // Detect token reuse — revoke entire family
        if (existingToken.IsUsed || existingToken.IsRevoked)
        {
            _logger.LogWarning("Refresh token reuse detected for family {FamilyId}. Revoking entire family.", existingToken.FamilyId);
            await RevokeFamilyAsync(existingToken.FamilyId, "Concurrent reuse detected", cancellationToken);
            return Result<LoginResponse>.Failure("Token reuse detected. All sessions have been revoked.");
        }

        if (DateTime.UtcNow >= existingToken.ExpiresAt)
            return Result<LoginResponse>.Failure("Refresh token has expired.");

        var user = existingToken.User;
        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("User not found or inactive.");

        // Mark current token as used (rotation)
        existingToken.IsUsed = true;
        existingToken.UpdatedAt = DateTime.UtcNow;

        var perms = await _permissions.GetUserPermissionsAsync(user.TenantId, user.Id, cancellationToken);
        var roles = perms.Roles;
        var permissions = perms.Permissions;

        var newAccessToken = _jwtService.GenerateAccessToken(user, roles, permissions);
        var (newRefreshTokenValue, _) = _jwtService.GenerateRefreshToken(existingToken.FamilyId);

        existingToken.ReplacedByToken = newRefreshTokenValue;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = newRefreshTokenValue,
            FamilyId = existingToken.FamilyId, // same family
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress
        };

        await _db.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            newAccessToken,
            newRefreshTokenValue,
            user.Id,
            user.Email,
            user.FullName,
            roles
        ));
    }

    private async Task RevokeFamilyAsync(string familyId, string reason, CancellationToken cancellationToken)
    {
        var familyTokens = await _db.RefreshTokens
            .Where(r => r.FamilyId == familyId && !r.IsRevoked && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var token in familyTokens)
        {
            token.IsRevoked = true;
            token.RevokedReason = reason;
            token.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
