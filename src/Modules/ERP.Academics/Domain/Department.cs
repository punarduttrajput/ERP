using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class Department : TenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Guid? HeadOfDepartmentUserId { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<AcademicProgram> Programs { get; set; } = new List<AcademicProgram>();
}
