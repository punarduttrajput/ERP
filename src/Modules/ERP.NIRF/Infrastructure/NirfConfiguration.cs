using ERP.NIRF.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.NIRF.Infrastructure;

public class NirfSubmissionConfiguration : IEntityTypeConfiguration<NirfSubmission>
{
    public void Configure(EntityTypeBuilder<NirfSubmission> builder)
    {
        builder.ToTable("nirf_submissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.OverallScore).HasColumnType("decimal(6,2)");
        builder.HasIndex(x => new { x.TenantId, x.RankingYear }).IsUnique();
        builder.HasMany(x => x.ParameterScores)
               .WithOne(x => x.Submission)
               .HasForeignKey(x => x.SubmissionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NirfParameterScoreConfiguration : IEntityTypeConfiguration<NirfParameterScore>
{
    public void Configure(EntityTypeBuilder<NirfParameterScore> builder)
    {
        builder.ToTable("nirf_parameter_scores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Parameter).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RawScore).HasColumnType("decimal(6,2)");
        builder.Property(x => x.WeightedScore).HasColumnType("decimal(6,2)");
        builder.Property(x => x.Weight).HasColumnType("decimal(4,2)");
        builder.Property(x => x.DataJson).HasMaxLength(10000).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.SubmissionId, x.Parameter }).IsUnique();
    }
}

public class NirfRankEntryConfiguration : IEntityTypeConfiguration<NirfRankEntry>
{
    public void Configure(EntityTypeBuilder<NirfRankEntry> builder)
    {
        builder.ToTable("nirf_rank_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Score).HasColumnType("decimal(6,2)");
        builder.Property(x => x.TeachingLearningScore).HasColumnType("decimal(6,2)");
        builder.Property(x => x.ResearchScore).HasColumnType("decimal(6,2)");
        builder.Property(x => x.GraduationOutcomesScore).HasColumnType("decimal(6,2)");
        builder.Property(x => x.OutreachScore).HasColumnType("decimal(6,2)");
        builder.Property(x => x.PerceptionScore).HasColumnType("decimal(6,2)");
        builder.HasIndex(x => new { x.TenantId, x.RankingYear, x.Category }).IsUnique();
    }
}
