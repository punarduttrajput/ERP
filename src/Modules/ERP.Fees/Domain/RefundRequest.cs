using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class RefundRequest : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
    public Guid InitiatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? GatewayRefundId { get; set; }
    public string? RejectionReason { get; set; }
}
