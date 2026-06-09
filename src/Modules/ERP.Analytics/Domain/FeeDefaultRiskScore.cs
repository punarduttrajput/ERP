using ERP.Shared.Domain;

namespace ERP.Analytics.Domain;

public class FeeDefaultRiskScore : TenantEntity
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public decimal TotalDue { get; set; }
    public int OverdueDays { get; set; }
    public int PreviousDefaultCount { get; set; }
    public decimal RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public DateTime ComputedAt { get; set; }
}
