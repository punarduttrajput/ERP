using ERP.RBAC.Domain;
using ERP.RBAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.RBAC.Application.Queries;

public sealed class GetMenuForUserHandler : IRequestHandler<GetMenuForUserQuery, Result<List<MenuItemDto>>>
{
    private readonly IRbacDbContext _db;
    private readonly PermissionCacheService _permissionCache;

    public GetMenuForUserHandler(IRbacDbContext db, PermissionCacheService permissionCache)
    {
        _db = db;
        _permissionCache = permissionCache;
    }

    public async Task<Result<List<MenuItemDto>>> Handle(GetMenuForUserQuery request, CancellationToken cancellationToken)
    {
        // Get the user's tenant from their roles (or use context)
        var userRole = await _db.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId, cancellationToken);

        if (userRole is null)
            return Result<List<MenuItemDto>>.Success(new List<MenuItemDto>());

        var tenantId = userRole.TenantId;
        var userPerms = await _permissionCache.GetUserPermissionsAsync(tenantId, request.UserId, cancellationToken);
        var permissionSet = new HashSet<string>(userPerms.Permissions, StringComparer.OrdinalIgnoreCase);

        var allMenuItems = await _db.MenuItems
            .AsNoTracking()
            .Where(m => m.IsVisible && m.ParentId == null)
            .Include(m => m.Children)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);

        var accessibleItems = allMenuItems
            .Where(m => string.IsNullOrEmpty(m.RequiredPermission) || permissionSet.Contains(m.RequiredPermission))
            .Select(m => MapToDto(m, permissionSet))
            .ToList();

        return Result<List<MenuItemDto>>.Success(accessibleItems);
    }

    private static MenuItemDto MapToDto(MenuItem item, HashSet<string> userPermissions)
    {
        var accessibleChildren = item.Children
            .Where(c => c.IsVisible && (string.IsNullOrEmpty(c.RequiredPermission) || userPermissions.Contains(c.RequiredPermission)))
            .OrderBy(c => c.Order)
            .Select(c => MapToDto(c, userPermissions))
            .ToList();

        return new MenuItemDto(item.Id, item.Label, item.Icon, item.Route, item.Order, accessibleChildren);
    }
}
