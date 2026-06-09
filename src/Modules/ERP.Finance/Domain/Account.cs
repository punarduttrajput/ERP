using ERP.Shared.Domain;

namespace ERP.Finance.Domain;

public class Account : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsControl { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal Balance { get; set; } = 0m;

    public Account? ParentAccount { get; set; }
    public ICollection<Account> ChildAccounts { get; set; } = new List<Account>();
}
