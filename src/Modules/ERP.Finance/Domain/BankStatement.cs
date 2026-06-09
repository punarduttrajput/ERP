using ERP.Shared.Domain;

namespace ERP.Finance.Domain;

public class BankStatement : TenantEntity
{
    public Guid AccountId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public DateOnly StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }

    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}
