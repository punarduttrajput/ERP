using ERP.Shared.Domain;

namespace ERP.Finance.Domain;

public class BankStatementLine : TenantEntity
{
    public Guid StatementId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; } = 0m;
    public decimal Credit { get; set; } = 0m;
    public decimal Balance { get; set; }
    public ReconciliationStatus ReconciliationStatus { get; set; } = ReconciliationStatus.Unreconciled;
    public Guid? MatchedJournalLineId { get; set; }

    public BankStatement? Statement { get; set; }
}
