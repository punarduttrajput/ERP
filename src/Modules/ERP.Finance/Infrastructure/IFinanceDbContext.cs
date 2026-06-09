using ERP.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Infrastructure;

public interface IFinanceDbContext
{
    DbSet<Account> GlAccounts { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalLine> JournalLines { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<BudgetLine> BudgetLines { get; }
    DbSet<BankStatement> BankStatements { get; }
    DbSet<BankStatementLine> BankStatementLines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
