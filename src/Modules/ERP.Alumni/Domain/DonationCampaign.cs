using ERP.Shared.Domain;

namespace ERP.Alumni.Domain;

public class DonationCampaign : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CollectedAmount { get; set; } = 0;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool Section80GEligible { get; set; } = false;
    public string? Section80GRegistrationNumber { get; set; }
    public ICollection<DonationPledge> Pledges { get; set; } = new List<DonationPledge>();
}
