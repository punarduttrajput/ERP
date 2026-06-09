using ERP.Fees.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Fees.Infrastructure;

public class FeeStructureConfiguration : IEntityTypeConfiguration<FeeStructure>
{
    public void Configure(EntityTypeBuilder<FeeStructure> builder)
    {
        builder.ToTable("fee_structures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProgramName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.TenantId, x.ProgramId, x.SemesterNumber, x.Category, x.AcademicYear }).IsUnique();
        builder.HasMany(x => x.Components).WithOne(x => x.FeeStructure).HasForeignKey(x => x.FeeStructureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.InstallmentSchedules).WithOne(x => x.FeeStructure).HasForeignKey(x => x.FeeStructureId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FeeComponentConfiguration : IEntityTypeConfiguration<FeeComponent>
{
    public void Configure(EntityTypeBuilder<FeeComponent> builder)
    {
        builder.ToTable("fee_components");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.IsRefundable).HasDefaultValue(false);
        builder.HasIndex(x => new { x.TenantId, x.FeeStructureId });
    }
}

public class InstallmentScheduleConfiguration : IEntityTypeConfiguration<InstallmentSchedule>
{
    public void Configure(EntityTypeBuilder<InstallmentSchedule> builder)
    {
        builder.ToTable("installment_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LateFinePerDay).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MaxLateFine).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => new { x.TenantId, x.FeeStructureId });
    }
}

public class StudentFeeAccountConfiguration : IEntityTypeConfiguration<StudentFeeAccount>
{
    public void Configure(EntityTypeBuilder<StudentFeeAccount> builder)
    {
        builder.ToTable("student_fee_accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.NetAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.DueAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.IsFullyPaid).HasDefaultValue(false);
        builder.HasIndex(x => new { x.TenantId, x.StudentId, x.AcademicYear, x.SemesterNumber }).IsUnique();
        builder.HasMany(x => x.Installments).WithOne(x => x.Account).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Payments).WithOne(x => x.Account).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FeeInstallmentConfiguration : IEntityTypeConfiguration<FeeInstallment>
{
    public void Configure(EntityTypeBuilder<FeeInstallment> builder)
    {
        builder.ToTable("fee_installments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BaseAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LateFine).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.TotalDue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.IsPaid).HasDefaultValue(false);
        builder.HasIndex(x => new { x.TenantId, x.AccountId });
    }
}

public class FeePaymentConfiguration : IEntityTypeConfiguration<FeePayment>
{
    public void Configure(EntityTypeBuilder<FeePayment> builder)
    {
        builder.ToTable("fee_payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GatewayOrderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.GatewayPaymentId).HasMaxLength(100);
        builder.Property(x => x.GatewaySignature).HasMaxLength(500);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.PaymentMethod).HasMaxLength(50);
        builder.Property(x => x.ReceiptNumber).HasMaxLength(50);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.TenantId, x.AccountId });
        builder.HasIndex(x => x.GatewayOrderId);
    }
}

public class ScholarshipConfiguration : IEntityTypeConfiguration<Scholarship>
{
    public void Configure(EntityTypeBuilder<Scholarship> builder)
    {
        builder.ToTable("scholarships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ScholarshipType).HasConversion<int>();
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountPercent).HasColumnType("decimal(8,4)");
        builder.Property(x => x.MinMeritScore).HasColumnType("decimal(8,2)");
        builder.Property(x => x.EligibleCategories).HasMaxLength(200);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => x.TenantId);
    }
}

public class StudentScholarshipConfiguration : IEntityTypeConfiguration<StudentScholarship>
{
    public void Configure(EntityTypeBuilder<StudentScholarship> builder)
    {
        builder.ToTable("student_scholarships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DiscountApplied).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => new { x.TenantId, x.StudentId, x.ScholarshipId, x.AcademicYear }).IsUnique();
        builder.HasOne(x => x.Scholarship).WithMany().HasForeignKey(x => x.ScholarshipId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.ToTable("refund_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.GatewayRefundId).HasMaxLength(100);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.TenantId, x.StudentId });
        builder.HasIndex(x => new { x.TenantId, x.PaymentId });
    }
}
