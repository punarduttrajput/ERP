using ERP.Shared.Domain;

namespace ERP.Alumni.Domain;

public class DonationPledge : TenantEntity
{
    public Guid CampaignId { get; set; }
    public Guid AlumniId { get; set; }
    public string AlumniName { get; set; } = string.Empty;
    public string AlumniEmail { get; set; } = string.Empty;
    public decimal PledgedAmount { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public PledgeStatus Status { get; set; }
    public DateTime PledgedAt { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public string? ReceiptNumber { get; set; }
    public DonationCampaign? Campaign { get; set; }
}
