using ERP.HRMS.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.HRMS.Infrastructure;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Designation).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EmploymentType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(20);
        builder.Property(x => x.Gender).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PanNumber).HasMaxLength(20);
        builder.Property(x => x.AadharNumber).HasMaxLength(20);

        builder.HasIndex(x => new { x.TenantId, x.EmployeeCode }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.Employee)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("employee_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BlobUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId });
    }
}

public class RecruitmentRequisitionConfiguration : IEntityTypeConfiguration<RecruitmentRequisition>
{
    public void Configure(EntityTypeBuilder<RecruitmentRequisition> builder)
    {
        builder.ToTable("recruitment_requisitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Designation).HasMaxLength(200).IsRequired();
        builder.Property(x => x.JobDescription).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.IsPublished).HasDefaultValue(false);

        builder.HasMany(x => x.Applications)
            .WithOne(x => x.Requisition)
            .HasForeignKey(x => x.RequisitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("job_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ApplicantName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ApplicantEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ApplicantMobile).HasMaxLength(20);
        builder.Property(x => x.ResumeBlobUrl).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.InterviewNotes).HasMaxLength(2000);
        builder.Property(x => x.OfferSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.RequisitionId });
    }
}

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("leave_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("leave_balances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.UsedDays).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
        builder.Property(x => x.PendingDays).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
        // AvailableDays is computed — not persisted
        builder.Ignore(x => x.AvailableDays);

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
    }
}

public class LeaveApplicationConfiguration : IEntityTypeConfiguration<LeaveApplication>
{
    public void Configure(EntityTypeBuilder<LeaveApplication> builder)
    {
        builder.ToTable("leave_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId });
    }
}

public class SalaryStructureConfiguration : IEntityTypeConfiguration<SalaryStructure>
{
    public void Configure(EntityTypeBuilder<SalaryStructure> builder)
    {
        builder.ToTable("salary_structures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasMany(x => x.Components)
            .WithOne(x => x.SalaryStructure)
            .HasForeignKey(x => x.SalaryStructureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.ToTable("salary_components");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ComponentType).HasConversion<int>();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Percentage).HasColumnType("decimal(8,4)");
        builder.Property(x => x.BaseComponent).HasMaxLength(100);

        builder.HasIndex(x => new { x.TenantId, x.SalaryStructureId });
    }
}

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("payroll_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IsPostedToGl).HasDefaultValue(false);
        builder.Property(x => x.TotalGrossPay).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalDeductions).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalNetPay).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.TenantId, x.Month, x.Year }).IsUnique();

        builder.HasMany(x => x.Entries)
            .WithOne(x => x.PayrollRun)
            .HasForeignKey(x => x.PayrollRunId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollEntryConfiguration : IEntityTypeConfiguration<PayrollEntry>
{
    public void Configure(EntityTypeBuilder<PayrollEntry> builder)
    {
        builder.ToTable("payroll_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.EmployeeName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.GrossPay).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PfEmployee).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PfEmployer).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EsiEmployee).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EsiEmployer).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TdsAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalDeductions).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NetPay).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxRegime).HasMaxLength(10).IsRequired();
        builder.Property(x => x.PayslipGenerated).HasDefaultValue(false);

        builder.HasIndex(x => new { x.TenantId, x.PayrollRunId });
    }
}

public class AppraisalConfiguration : IEntityTypeConfiguration<Appraisal>
{
    public void Configure(EntityTypeBuilder<Appraisal> builder)
    {
        builder.ToTable("appraisals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.SelfAssessment).HasMaxLength(5000);
        builder.Property(x => x.SelfRating).HasColumnType("decimal(3,1)");
        builder.Property(x => x.ManagerReview).HasMaxLength(5000);
        builder.Property(x => x.ManagerRating).HasColumnType("decimal(3,1)");
        builder.Property(x => x.HrComments).HasMaxLength(2000);
        builder.Property(x => x.FinalRating).HasColumnType("decimal(3,1)");

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ReviewYear }).IsUnique();
    }
}
