using ERP.RBAC.Domain;
using ERP.RBAC.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.RBAC.Application.Commands;

public sealed class AssignUserRoleHandler : IRequestHandler<AssignUserRoleCommand, Result>
{
    private readonly IRbacDbContext _db;
    private readonly PermissionCacheService _permissionCache;
    private readonly ICurrentTenant _currentTenant;

    public AssignUserRoleHandler(IRbacDbContext db, PermissionCacheService permissionCache, ICurrentTenant currentTenant)
    {
        _db = db;
        _permissionCache = permissionCache;
        _currentTenant = currentTenant;
    }

    public async Task<Result> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
            return Result.Failure("Tenant context is required.");

        var tenantId = _currentTenant.TenantId.Value;

        var roleExists = await _db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
            return Result.Failure("Role not found.");

        var alreadyAssigned = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);

        if (alreadyAssigned)
            return Result.Success();

        var userRole = new UserRole
        {
            TenantId = tenantId,
            UserId = request.UserId,
            RoleId = request.RoleId
        };

        await _db.UserRoles.AddAsync(userRole, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // Invalidate this specific user's permission cache
        await _permissionCache.InvalidateAsync(tenantId, request.UserId, cancellationToken);

        return Result.Success();
    }
}
