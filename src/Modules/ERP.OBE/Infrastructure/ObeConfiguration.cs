using ERP.OBE.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.OBE.Infrastructure;

public class CoPoMappingConfiguration : IEntityTypeConfiguration<CoPoMapping>
{
    public void Configure(EntityTypeBuilder<CoPoMapping> builder)
    {
        builder.ToTable("co_po_mappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CourseOutcomeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ProgramOutcomeCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.SubjectId, x.CourseOutcomeCode, x.ProgramOutcomeCode }).IsUnique();
    }
}

public class CoPsoMappingConfiguration : IEntityTypeConfiguration<CoPsoMapping>
{
    public void Configure(EntityTypeBuilder<CoPsoMapping> builder)
    {
        builder.ToTable("co_pso_mappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CourseOutcomeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PsoCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.SubjectId, x.CourseOutcomeCode, x.PsoCode }).IsUnique();
    }
}

public class DirectAttainmentConfiguration : IEntityTypeConfiguration<DirectAttainment>
{
    public void Configure(EntityTypeBuilder<DirectAttainment> builder)
    {
        builder.ToTable("direct_attainments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CourseOutcomeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AttainmentPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ThresholdPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Level).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.SubjectId, x.CourseOutcomeCode, x.SemesterId }).IsUnique();
    }
}

public class IndirectAttainmentSurveyConfiguration : IEntityTypeConfiguration<IndirectAttainmentSurvey>
{
    public void Configure(EntityTypeBuilder<IndirectAttainmentSurvey> builder)
    {
        builder.ToTable("indirect_surveys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.AggregatedScore).HasColumnType("decimal(5,2)");
        builder.HasMany(x => x.Questions)
               .WithOne(x => x.Survey)
               .HasForeignKey(x => x.SurveyId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.TenantId, x.SubjectId, x.SemesterId });
    }
}

public class SurveyQuestionConfiguration : IEntityTypeConfiguration<SurveyQuestion>
{
    public void Configure(EntityTypeBuilder<SurveyQuestion> builder)
    {
        builder.ToTable("survey_questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CourseOutcomeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.QuestionText).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.SurveyId });
    }
}

public class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("survey_responses");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.SurveyId, x.StudentId, x.QuestionId }).IsUnique();
    }
}

public class AttainmentGapConfiguration : IEntityTypeConfiguration<AttainmentGap>
{
    public void Configure(EntityTypeBuilder<AttainmentGap> builder)
    {
        builder.ToTable("attainment_gaps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CourseOutcomeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DirectAttainmentPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.IndirectAttainmentPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CombinedAttainmentPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TargetPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.GapPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Level).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.SubjectId, x.CourseOutcomeCode, x.SemesterId });
    }
}

public class ActionPlanConfiguration : IEntityTypeConfiguration<ActionPlan>
{
    public void Configure(EntityTypeBuilder<ActionPlan> builder)
    {
        builder.ToTable("action_plans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CourseOutcomeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Outcome).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.SubjectId, x.CourseOutcomeCode });
    }
}
