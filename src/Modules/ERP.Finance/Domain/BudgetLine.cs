using ERP.Shared.Domain;

namespace ERP.Finance.Domain;

public class BudgetLine : TenantEntity
{
    public Guid BudgetId { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; } = 0m;

    public Budget? Budget { get; set; }
}
