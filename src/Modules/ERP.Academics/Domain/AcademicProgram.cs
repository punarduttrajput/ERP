using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class AcademicProgram : TenantEntity
{
    public Guid DepartmentId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int DurationYears { get; set; }
    public int TotalSemesters { get; set; }
    public string DegreeType { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public Department? Department { get; set; }
    public ICollection<ProgramOutcome> ProgramOutcomes { get; set; } = new List<ProgramOutcome>();
    public ICollection<ProgramSpecificOutcome> ProgramSpecificOutcomes { get; set; } = new List<ProgramSpecificOutcome>();
}
