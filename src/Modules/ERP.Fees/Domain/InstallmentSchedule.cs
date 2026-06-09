using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class InstallmentSchedule : TenantEntity
{
    public Guid FeeStructureId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal LateFinePerDay { get; set; }
    public decimal MaxLateFine { get; set; }
    public FeeStructure? FeeStructure { get; set; }
}
