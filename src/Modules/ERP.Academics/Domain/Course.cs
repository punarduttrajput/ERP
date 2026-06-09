using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class Course : TenantEntity
{
    public Guid ProgramId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
