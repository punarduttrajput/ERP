using ERP.Analytics.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Analytics.Infrastructure;

public class StudentRiskScoreConfiguration : IEntityTypeConfiguration<StudentRiskScore>
{
    public void Configure(EntityTypeBuilder<StudentRiskScore> builder)
    {
        builder.ToTable("student_risk_scores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProgramName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AttendancePercent).HasPrecision(5, 2);
        builder.Property(x => x.AverageMarksPercent).HasPrecision(5, 2);
        builder.Property(x => x.RiskScore).HasPrecision(5, 2);
        builder.Property(x => x.RiskLevel).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.StudentId, x.SemesterId }).IsUnique();
    }
}

public class FeeDefaultRiskScoreConfiguration : IEntityTypeConfiguration<FeeDefaultRiskScore>
{
    public void Configure(EntityTypeBuilder<FeeDefaultRiskScore> builder)
    {
        builder.ToTable("fee_default_risk_scores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TotalDue).HasPrecision(18, 2);
        builder.Property(x => x.RiskScore).HasPrecision(5, 2);
        builder.Property(x => x.RiskLevel).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.StudentId, x.AcademicYear }).IsUnique();
    }
}

public class PlacementScoreConfiguration : IEntityTypeConfiguration<PlacementScore>
{
    public void Configure(EntityTypeBuilder<PlacementScore> builder)
    {
        builder.ToTable("placement_scores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProgramName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Cgpa).HasPrecision(4, 2);
        builder.Property(x => x.AttendancePercent).HasPrecision(5, 2);
        builder.Property(x => x.PlacementScoreValue).HasPrecision(5, 2);
        builder.Property(x => x.PlacementProbabilityPercent).HasPrecision(5, 2);
        builder.HasIndex(x => new { x.TenantId, x.StudentId, x.AcademicYear }).IsUnique();
    }
}
