using ERP.Shared.Domain;

namespace ERP.ABC.Domain;

public class AcademicPathway : TenantEntity
{
    public Guid StudentId { get; set; }
    public PathwayType PathwayType { get; set; }
    public int RequiredCredits { get; set; }
    public int CreditsEarned { get; set; }
    public bool IsEligible { get; set; }
    public DateTime? SelectedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public int? ExitYear { get; set; }
    public string Status { get; set; } = string.Empty;
}
