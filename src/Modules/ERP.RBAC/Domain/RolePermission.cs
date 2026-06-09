using ERP.Shared.Domain;

namespace ERP.RBAC.Domain;

public class RolePermission : TenantEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
