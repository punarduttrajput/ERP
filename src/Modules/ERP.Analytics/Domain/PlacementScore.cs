using ERP.Shared.Domain;

namespace ERP.Analytics.Domain;

public class PlacementScore : TenantEntity
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public decimal Cgpa { get; set; }
    public int ActiveBacklogs { get; set; }
    public decimal AttendancePercent { get; set; }
    public decimal PlacementScoreValue { get; set; }
    public decimal PlacementProbabilityPercent { get; set; }
    public DateTime ComputedAt { get; set; }
}
