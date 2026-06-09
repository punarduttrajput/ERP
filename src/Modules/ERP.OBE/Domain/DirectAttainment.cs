using ERP.Shared.Domain;

namespace ERP.OBE.Domain;

public class DirectAttainment : TenantEntity
{
    public Guid SubjectId { get; set; }
    public string CourseOutcomeCode { get; set; } = string.Empty;
    public Guid SemesterId { get; set; }
    public int AcademicYear { get; set; }
    public int TotalStudents { get; set; }
    public int StudentsAttained { get; set; }
    public decimal AttainmentPercent { get; set; }
    public decimal ThresholdPercent { get; set; }
    public AttainmentLevel Level { get; set; }
    public DateTime ComputedAt { get; set; }
}
