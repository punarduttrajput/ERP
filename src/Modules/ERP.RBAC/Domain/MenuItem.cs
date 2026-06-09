using ERP.Shared.Domain;

namespace ERP.RBAC.Domain;

public class MenuItem : TenantEntity
{
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public string? RequiredPermission { get; set; }
    public int Order { get; set; } = 0;
    public Guid? ParentId { get; set; }
    public bool IsVisible { get; set; } = true;

    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
}
