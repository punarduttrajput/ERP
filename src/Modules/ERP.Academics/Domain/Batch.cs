using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class Batch : TenantEntity
{
    public Guid ProgramId { get; set; }
    public Guid AcademicYearId { get; set; }
    public string Name { get; set; } = default!;
    public int AdmissionYear { get; set; }
    public int CurrentSemester { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
