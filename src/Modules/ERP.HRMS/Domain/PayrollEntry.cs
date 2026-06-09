using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class PayrollEntry : TenantEntity
{
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public Guid SalaryStructureId { get; set; }
    public decimal GrossPay { get; set; }
    public decimal PfEmployee { get; set; }
    public decimal PfEmployer { get; set; }
    public decimal? EsiEmployee { get; set; }
    public decimal? EsiEmployer { get; set; }
    public decimal TdsAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string TaxRegime { get; set; } = "New";
    public bool PayslipGenerated { get; set; } = false;

    public PayrollRun? PayrollRun { get; set; }
}
