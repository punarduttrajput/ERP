using ERP.Finance.Domain;

namespace ERP.Finance.Infrastructure;

public static class DefaultChartOfAccountsSeeder
{
    private static readonly (string Code, string Name, AccountType Type)[] DefaultAccounts =
    {
        ("1000", "Cash and Bank",                  AccountType.Asset),
        ("1010", "Main Bank Account",              AccountType.Asset),
        ("1100", "Accounts Receivable - Students", AccountType.Asset),
        ("2000", "Accounts Payable",               AccountType.Liability),
        ("2100", "Deferred Fee Income",            AccountType.Liability),
        ("3000", "Retained Earnings",              AccountType.Equity),
        ("4000", "Fee Income",                     AccountType.Income),
        ("4100", "Other Income",                   AccountType.Income),
        ("5000", "Salaries",                       AccountType.Expense),
        ("5100", "Administrative Expenses",        AccountType.Expense),
        ("5200", "Academic Expenses",              AccountType.Expense),
    };

    public static async Task SeedAsync(IFinanceDbContext context, Guid tenantId, CancellationToken cancellationToken = default)
    {
        foreach (var (code, name, type) in DefaultAccounts)
        {
            var exists = context.GlAccounts.Any(a => a.TenantId == tenantId && a.Code == code);
            if (exists)
                continue;

            context.GlAccounts.Add(new Account
            {
                TenantId = tenantId,
                Code = code,
                Name = name,
                AccountType = type,
                IsActive = true,
                IsControl = false,
                Balance = 0m
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
