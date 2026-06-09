using ERP.RBAC.Domain;
using ERP.RBAC.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.RBAC.Application.Commands;

public sealed class AssignPermissionHandler : IRequestHandler<AssignPermissionCommand, Result>
{
    private readonly IRbacDbContext _db;
    private readonly PermissionCacheService _permissionCache;
    private readonly ICurrentTenant _currentTenant;

    public AssignPermissionHandler(IRbacDbContext db, PermissionCacheService permissionCache, ICurrentTenant currentTenant)
    {
        _db = db;
        _permissionCache = permissionCache;
        _currentTenant = currentTenant;
    }

    public async Task<Result> Handle(AssignPermissionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
            return Result.Failure("Tenant context is required.");

        var tenantId = _currentTenant.TenantId.Value;

        var roleExists = await _db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
            return Result.Failure("Role not found.");

        var permExists = await _db.Permissions.AnyAsync(p => p.Id == request.PermissionId, cancellationToken);
        if (!permExists)
            return Result.Failure("Permission not found.");

        var alreadyAssigned = await _db.RolePermissions
            .AnyAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId, cancellationToken);

        if (alreadyAssigned)
            return Result.Success();

        var rolePermission = new RolePermission
        {
            TenantId = tenantId,
            RoleId = request.RoleId,
            PermissionId = request.PermissionId
        };

        await _db.RolePermissions.AddAsync(rolePermission, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // Invalidate cache for all users in this tenant (their permissions changed)
        await _permissionCache.InvalidateTenantAsync(tenantId, cancellationToken);

        return Result.Success();
    }
}
