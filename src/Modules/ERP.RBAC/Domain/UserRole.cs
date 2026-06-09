using ERP.Shared.Domain;

namespace ERP.RBAC.Domain;

public class UserRole : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public Role? Role { get; set; }
}
