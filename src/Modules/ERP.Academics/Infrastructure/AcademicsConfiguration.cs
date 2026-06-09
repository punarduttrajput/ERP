using ERP.Academics.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Academics.Infrastructure;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.HeadOfDepartmentUserId).HasColumnType("char(36)");
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasMany(x => x.Programs).WithOne(x => x.Department).HasForeignKey(x => x.DepartmentId);
    }
}

public class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("academic_programs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.DepartmentId).HasColumnType("char(36)");
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DegreeType).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasMany(x => x.ProgramOutcomes).WithOne().HasForeignKey(x => x.ProgramId);
        builder.HasMany(x => x.ProgramSpecificOutcomes).WithOne().HasForeignKey(x => x.ProgramId);
    }
}

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.ProgramId).HasColumnType("char(36)");
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.ProgramId).HasColumnType("char(36)");
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SubjectType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SyllabusUrl).HasMaxLength(1000);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasMany(x => x.CourseOutcomes).WithOne(x => x.Subject).HasForeignKey(x => x.SubjectId);
    }
}

public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.ToTable("academic_years");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.Label).HasMaxLength(20).IsRequired();
        builder.HasMany(x => x.Semesters).WithOne(x => x.AcademicYear).HasForeignKey(x => x.AcademicYearId);
    }
}

public class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    public void Configure(EntityTypeBuilder<Semester> builder)
    {
        builder.ToTable("semesters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.AcademicYearId).HasColumnType("char(36)");
        builder.Property(x => x.Label).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.AcademicYearId, x.Number }).IsUnique();
    }
}

public class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.ToTable("batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.ProgramId).HasColumnType("char(36)");
        builder.Property(x => x.AcademicYearId).HasColumnType("char(36)");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
    }
}

public class CurriculumEntryConfiguration : IEntityTypeConfiguration<CurriculumEntry>
{
    public void Configure(EntityTypeBuilder<CurriculumEntry> builder)
    {
        builder.ToTable("curriculum_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.ProgramId).HasColumnType("char(36)");
        builder.Property(x => x.SubjectId).HasColumnType("char(36)");
        builder.HasIndex(x => new { x.TenantId, x.ProgramId, x.SemesterNumber, x.SubjectId }).IsUnique();
        builder.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
    }
}

public class CourseOutcomeConfiguration : IEntityTypeConfiguration<CourseOutcome>
{
    public void Configure(EntityTypeBuilder<CourseOutcome> builder)
    {
        builder.ToTable("course_outcomes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.SubjectId).HasColumnType("char(36)");
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
    }
}

public class ProgramOutcomeConfiguration : IEntityTypeConfiguration<ProgramOutcome>
{
    public void Configure(EntityTypeBuilder<ProgramOutcome> builder)
    {
        builder.ToTable("program_outcomes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.ProgramId).HasColumnType("char(36)");
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
    }
}

public class ProgramSpecificOutcomeConfiguration : IEntityTypeConfiguration<ProgramSpecificOutcome>
{
    public void Configure(EntityTypeBuilder<ProgramSpecificOutcome> builder)
    {
        builder.ToTable("program_specific_outcomes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.ProgramId).HasColumnType("char(36)");
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
    }
}
