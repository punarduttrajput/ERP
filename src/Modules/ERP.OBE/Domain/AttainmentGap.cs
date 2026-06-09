using ERP.Shared.Domain;

namespace ERP.OBE.Domain;

public class AttainmentGap : TenantEntity
{
    public Guid SubjectId { get; set; }
    public string CourseOutcomeCode { get; set; } = string.Empty;
    public Guid SemesterId { get; set; }
    public int AcademicYear { get; set; }
    public decimal DirectAttainmentPercent { get; set; }
    public decimal? IndirectAttainmentPercent { get; set; }
    public decimal CombinedAttainmentPercent { get; set; }
    public decimal TargetPercent { get; set; }
    public decimal GapPercent { get; set; }
    public AttainmentLevel Level { get; set; }
}
