using ERP.Exams.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Exams.Infrastructure;

public class ExamScheduleConfiguration : IEntityTypeConfiguration<ExamSchedule>
{
    public void Configure(EntityTypeBuilder<ExamSchedule> builder)
    {
        builder.ToTable("exam_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SemesterId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SubjectId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SubjectName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExamDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time(6)").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time(6)").IsRequired();
        builder.Property(x => x.Venue).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MaxMarks).HasDefaultValue(100).IsRequired();
        builder.Property(x => x.PassingMarks).HasDefaultValue(40).IsRequired();
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");

        builder.HasIndex(x => new { x.TenantId, x.SemesterId, x.SubjectId, x.ExamDate })
            .IsUnique()
            .HasDatabaseName("IX_exam_schedules_TenantId_SemesterId_SubjectId_ExamDate");

        builder.HasMany(x => x.SeatAllocations)
            .WithOne(s => s.ExamSchedule)
            .HasForeignKey(s => s.ExamScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SeatAllocationConfiguration : IEntityTypeConfiguration<SeatAllocation>
{
    public void Configure(EntityTypeBuilder<SeatAllocation> builder)
    {
        builder.ToTable("seat_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExamScheduleId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.StudentId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.RollNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SeatNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.HallTicketGenerated).HasDefaultValue(false);
        builder.Property(x => x.IsEligible).HasDefaultValue(true);
        builder.Property(x => x.IneligibilityReason).HasMaxLength(500);
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");

        builder.HasIndex(x => new { x.TenantId, x.ExamScheduleId, x.StudentId })
            .IsUnique()
            .HasDatabaseName("IX_seat_allocations_TenantId_ExamScheduleId_StudentId");
    }
}

public class GradingSchemeConfiguration : IEntityTypeConfiguration<GradingScheme>
{
    public void Configure(EntityTypeBuilder<GradingScheme> builder)
    {
        builder.ToTable("grading_schemes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsDefault).HasDefaultValue(false);
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");

        builder.HasMany(x => x.GradeRules)
            .WithOne(r => r.GradingScheme)
            .HasForeignKey(r => r.GradingSchemeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GradeRuleConfiguration : IEntityTypeConfiguration<GradeRule>
{
    public void Configure(EntityTypeBuilder<GradeRule> builder)
    {
        builder.ToTable("grade_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GradingSchemeId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.MinMarks).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(x => x.MaxMarks).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(x => x.GradeLetter).HasMaxLength(5).IsRequired();
        builder.Property(x => x.GradePoints).HasColumnType("decimal(4,2)").IsRequired();
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
    }
}

public class InternalMarkConfiguration : IEntityTypeConfiguration<InternalMark>
{
    public void Configure(EntityTypeBuilder<InternalMark> builder)
    {
        builder.ToTable("internal_marks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SubjectId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.StudentId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SemesterId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.Marks).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(x => x.MaxMarks).HasColumnType("decimal(6,2)").HasDefaultValue(50m);
        builder.Property(x => x.EnteredBy).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");

        builder.HasIndex(x => new { x.TenantId, x.SubjectId, x.StudentId, x.SemesterId })
            .IsUnique()
            .HasDatabaseName("IX_internal_marks_TenantId_SubjectId_StudentId_SemesterId");
    }
}

public class ExternalMarkConfiguration : IEntityTypeConfiguration<ExternalMark>
{
    public void Configure(EntityTypeBuilder<ExternalMark> builder)
    {
        builder.ToTable("external_marks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExamScheduleId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.StudentId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.Marks).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(x => x.MaxMarks).HasColumnType("decimal(6,2)").HasDefaultValue(100m);
        builder.Property(x => x.IsAbsent).HasDefaultValue(false);
        builder.Property(x => x.EnteredBy).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");

        builder.HasIndex(x => new { x.TenantId, x.ExamScheduleId, x.StudentId })
            .IsUnique()
            .HasDatabaseName("IX_external_marks_TenantId_ExamScheduleId_StudentId");
    }
}

public class StudentResultConfiguration : IEntityTypeConfiguration<StudentResult>
{
    public void Configure(EntityTypeBuilder<StudentResult> builder)
    {
        builder.ToTable("student_results");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StudentId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SemesterId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SubjectId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SubjectName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InternalMarks).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(x => x.ExternalMarks).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(x => x.TotalMarks).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(x => x.MaxMarks).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(x => x.GradeLetter).HasMaxLength(5).IsRequired();
        builder.Property(x => x.GradePoints).HasColumnType("decimal(4,2)").IsRequired();
        builder.Property(x => x.Credits).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.IsPublished).HasDefaultValue(false);
        builder.Property(x => x.PublishedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.GPA).HasColumnType("decimal(4,2)");
        builder.Property(x => x.CGPA).HasColumnType("decimal(4,2)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");

        builder.HasIndex(x => new { x.TenantId, x.StudentId, x.SemesterId, x.SubjectId })
            .IsUnique()
            .HasDatabaseName("IX_student_results_TenantId_StudentId_SemesterId_SubjectId");
    }
}

public class ArrearRegistrationConfiguration : IEntityTypeConfiguration<ArrearRegistration>
{
    public void Configure(EntityTypeBuilder<ArrearRegistration> builder)
    {
        builder.ToTable("arrear_registrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StudentId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SubjectId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SemesterId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.ExamSemesterId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.IsApproved).HasDefaultValue(false);
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
    }
}
