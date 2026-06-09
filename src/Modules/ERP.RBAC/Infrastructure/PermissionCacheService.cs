using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.RBAC.Infrastructure;

public sealed class PermissionCacheService : IPermissionService
{
    private readonly ICacheService _cache;
    private readonly IRbacDbContext _db;
    private readonly ILogger<PermissionCacheService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public PermissionCacheService(ICacheService cache, IRbacDbContext db, ILogger<PermissionCacheService> logger)
    {
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    public async Task<ERP.Shared.Application.Contracts.UserPermissions> GetUserPermissionsAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(tenantId, userId);
        var cached = await _cache.GetAsync<UserPermissions>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var roles = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role!.Name)
            .ToListAsync(cancellationToken);

        var permissions = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Select(rp => rp.Permission!.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = new ERP.Shared.Application.Contracts.UserPermissions
        {
            Roles = roles.ToArray(),
            Permissions = permissions.ToArray()
        };

        await _cache.SetAsync(cacheKey, result, CacheTtl, cancellationToken);
        return result;
    }

    public async Task InvalidateAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(tenantId, userId);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogDebug("Permission cache invalidated for user {UserId} in tenant {TenantId}", userId, tenantId);
    }

    public async Task InvalidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var pattern = $"perm:{tenantId}:*";
        await _cache.RemoveByPatternAsync(pattern, cancellationToken);
        _logger.LogInformation("All permission caches invalidated for tenant {TenantId}", tenantId);
    }

    private static string GetCacheKey(Guid tenantId, Guid userId) => $"perm:{tenantId}:{userId}";
}
