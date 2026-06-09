using ERP.RBAC.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.RBAC.Infrastructure;

public interface IRbacDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<MenuItem> MenuItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
