using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class AcademicYear : TenantEntity
{
    public string Label { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }

    public ICollection<Semester> Semesters { get; set; } = new List<Semester>();
}
