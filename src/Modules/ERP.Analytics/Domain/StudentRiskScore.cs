using ERP.Shared.Domain;

namespace ERP.Analytics.Domain;

public class StudentRiskScore : TenantEntity
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public Guid SemesterId { get; set; }
    public int AcademicYear { get; set; }
    public decimal AttendancePercent { get; set; }
    public decimal AverageMarksPercent { get; set; }
    public decimal RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public bool AttendanceFlag { get; set; }
    public bool MarksFlag { get; set; }
    public bool CombinedFlag { get; set; }
    public DateTime ComputedAt { get; set; }
    public DateTime? AlertSentAt { get; set; }
}
