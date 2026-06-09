using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class PayrollRun : TenantEntity
{
    public int Month { get; set; }
    public int Year { get; set; }
    public bool IsPostedToGl { get; set; } = false;
    public DateTime ProcessedAt { get; set; }
    public Guid ProcessedBy { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }

    public ICollection<PayrollEntry> Entries { get; set; } = new List<PayrollEntry>();
}
