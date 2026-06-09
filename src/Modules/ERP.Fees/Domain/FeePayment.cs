using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class FeePayment : TenantEntity
{
    public Guid AccountId { get; set; }
    public Guid? InstallmentId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
    public string? GatewayPaymentId { get; set; }
    public string? GatewaySignature { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? FailureReason { get; set; }
    public StudentFeeAccount? Account { get; set; }
}
