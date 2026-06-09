using ERP.Shared.Domain;

namespace ERP.RBAC.Domain;

public class Permission : TenantEntity
{
    public string Name { get; set; } = string.Empty; // e.g. "users:read"
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty; // e.g. "users"
    public string Action { get; set; } = string.Empty; // e.g. "read"

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
