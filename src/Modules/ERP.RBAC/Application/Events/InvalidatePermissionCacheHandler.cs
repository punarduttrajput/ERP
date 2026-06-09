using ERP.RBAC.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ERP.RBAC.Application.Events;

public sealed class InvalidatePermissionCacheOnRoleChange : INotificationHandler<RolePermissionChangedEvent>
{
    private readonly PermissionCacheService _permissionCache;
    private readonly ILogger<InvalidatePermissionCacheOnRoleChange> _logger;

    public InvalidatePermissionCacheOnRoleChange(PermissionCacheService permissionCache, ILogger<InvalidatePermissionCacheOnRoleChange> logger)
    {
        _permissionCache = permissionCache;
        _logger = logger;
    }

    public async Task Handle(RolePermissionChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Role permission changed for TenantId={TenantId}, RoleId={RoleId}. Invalidating tenant-wide permission cache.",
            notification.TenantId, notification.RoleId);
        await _permissionCache.InvalidateTenantAsync(notification.TenantId, cancellationToken);
    }
}

public sealed class InvalidatePermissionCacheOnUserRoleChange : INotificationHandler<UserRoleChangedEvent>
{
    private readonly PermissionCacheService _permissionCache;
    private readonly ILogger<InvalidatePermissionCacheOnUserRoleChange> _logger;

    public InvalidatePermissionCacheOnUserRoleChange(PermissionCacheService permissionCache, ILogger<InvalidatePermissionCacheOnUserRoleChange> logger)
    {
        _permissionCache = permissionCache;
        _logger = logger;
    }

    public async Task Handle(UserRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User role changed for UserId={UserId} in TenantId={TenantId}. Invalidating user permission cache.",
            notification.UserId, notification.TenantId);
        await _permissionCache.InvalidateAsync(notification.TenantId, notification.UserId, cancellationToken);
    }
}
