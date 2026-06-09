using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class Subject : TenantEntity
{
    public Guid ProgramId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Credits { get; set; }
    public int ContactHoursPerWeek { get; set; }
    public string SubjectType { get; set; } = default!;
    public string? SyllabusUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CourseOutcome> CourseOutcomes { get; set; } = new List<CourseOutcome>();
}
