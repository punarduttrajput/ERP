using ERP.Finance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Finance.Infrastructure;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("gl_accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AccountType).HasConversion<int>();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.Balance).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.ChildAccounts)
            .HasForeignKey(x => x.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntryNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.TotalDebit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalCredit).HasColumnType("decimal(18,2)");

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Entry)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("journal_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Debit).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.Credit).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.Narration).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.JournalEntryId });
    }
}

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DepartmentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TotalAllocated).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalSpent).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.IsLocked).HasDefaultValue(false);

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.AcademicYear }).IsUnique();

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Budget)
            .HasForeignKey(x => x.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    public void Configure(EntityTypeBuilder<BudgetLine> builder)
    {
        builder.ToTable("budget_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AllocatedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SpentAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

        builder.HasIndex(x => new { x.TenantId, x.BudgetId });
    }
}

public class BankStatementConfiguration : IEntityTypeConfiguration<BankStatement>
{
    public void Configure(EntityTypeBuilder<BankStatement> builder)
    {
        builder.ToTable("bank_statements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BankName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ClosingBalance).HasColumnType("decimal(18,2)");

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Statement)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.ToTable("bank_statement_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Debit).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.Credit).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.Balance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReconciliationStatus).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.StatementId });
    }
}
