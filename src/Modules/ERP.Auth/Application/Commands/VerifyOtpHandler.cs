using System.Security.Cryptography;
using System.Text;
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

public sealed class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, Result<LoginResponse>>
{
    private readonly IAuthDbContext _db;
    private readonly ICacheService _cache;
    private readonly IJwtService _jwtService;
    private readonly IPermissionService _permissions;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<VerifyOtpHandler> _logger;

    public VerifyOtpHandler(
        IAuthDbContext db, ICacheService cache, IJwtService jwtService,
        IPermissionService permissions, ICurrentTenant currentTenant,
        ILogger<VerifyOtpHandler> logger)
    {
        _db = db;
        _cache = cache;
        _jwtService = jwtService;
        _permissions = permissions;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
            return Result<LoginResponse>.Failure("Tenant context is required.");

        var mobile = request.MobileNumber.Trim();
        var cacheKey = $"otp:{_currentTenant.TenantId}:{mobile}";
        var storedHash = await _cache.GetAsync<string>(cacheKey, cancellationToken);

        if (storedHash is null)
            return Result<LoginResponse>.Failure("OTP not found or expired.");

        var inputHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Otp)));
        if (!inputHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase))
            return Result<LoginResponse>.Failure("Invalid OTP.");

        await _cache.RemoveAsync(cacheKey, cancellationToken);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.MobileNumber == mobile && !u.IsDeleted && u.IsActive, cancellationToken);

        if (user is null)
            return Result<LoginResponse>.Failure("No active user found for this mobile number.");

        var perms = await _permissions.GetUserPermissionsAsync(user.TenantId, user.Id, cancellationToken);
        var accessToken = _jwtService.GenerateAccessToken(user, perms.Roles, perms.Permissions);
        var (refreshTokenValue, familyId) = _jwtService.GenerateRefreshToken();

        await _db.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = refreshTokenValue,
            FamilyId = familyId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress
        }, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OTP login for {Mobile} in tenant {TenantId}", mobile, _currentTenant.TenantId);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken, refreshTokenValue, user.Id, user.Email, user.FullName, perms.Roles));
    }
}
