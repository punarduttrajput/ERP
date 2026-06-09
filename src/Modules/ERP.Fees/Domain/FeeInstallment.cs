using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class FeeInstallment : TenantEntity
{
    public Guid AccountId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal LateFine { get; set; } = 0;
    public decimal TotalDue { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAt { get; set; }
    public StudentFeeAccount? Account { get; set; }
}
