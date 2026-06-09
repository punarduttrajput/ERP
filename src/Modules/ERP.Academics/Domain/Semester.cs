using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class Semester : TenantEntity
{
    public Guid AcademicYearId { get; set; }
    public int Number { get; set; }
    public string Label { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }

    public AcademicYear? AcademicYear { get; set; }
}
