using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.UnitTests.Finance;

public class TestFinanceDbContext : DbContext, IFinanceDbContext
{
    private readonly Guid _tenantId;

    public TestFinanceDbContext(DbContextOptions<TestFinanceDbContext> options, Guid tenantId)
        : base(options)
    {
        _tenantId = tenantId;
    }

    public DbSet<Account> GlAccounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Self-referential Account hierarchy
        modelBuilder.Entity<Account>()
            .HasOne(a => a.ParentAccount)
            .WithMany(a => a.ChildAccounts)
            .HasForeignKey(a => a.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JournalEntry>()
            .HasMany(e => e.Lines)
            .WithOne(l => l.Entry)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Budget>()
            .HasMany(b => b.Lines)
            .WithOne(l => l.Budget)
            .HasForeignKey(l => l.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BankStatement>()
            .HasMany(s => s.Lines)
            .WithOne(l => l.Statement)
            .HasForeignKey(l => l.StatementId)
            .OnDelete(DeleteBehavior.Restrict);

        // No global query filter in tests — handlers filter by TenantId explicitly
    }
}
