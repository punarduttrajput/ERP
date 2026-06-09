using ERP.Shared.Domain;

namespace ERP.NAAC.Domain;

public class DvvQuery : TenantEntity
{
    public Guid SsrId { get; set; }
    public string QueryNumber { get; set; } = string.Empty;
    public string CriterionNumber { get; set; } = string.Empty;
    public string IndicatorNumber { get; set; } = string.Empty;
    public string QueryText { get; set; } = string.Empty;
    public string? Response { get; set; }
    public string? SupportingDocUrls { get; set; }
    public DvvStatus Status { get; set; } = DvvStatus.Received;
    public DateTime ReceivedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public Guid? RespondedBy { get; set; }
}
