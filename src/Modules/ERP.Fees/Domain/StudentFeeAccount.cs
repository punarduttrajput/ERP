using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class StudentFeeAccount : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid FeeStructureId { get; set; }
    public int AcademicYear { get; set; }
    public int SemesterNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal DueAmount { get; set; }
    public bool IsFullyPaid { get; set; } = false;
    public ICollection<FeeInstallment> Installments { get; set; } = new List<FeeInstallment>();
    public ICollection<FeePayment> Payments { get; set; } = new List<FeePayment>();
}
