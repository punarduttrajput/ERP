using ERP.Accreditation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Accreditation.Infrastructure;

public class EvidenceTagConfiguration : IEntityTypeConfiguration<EvidenceTag>
{
    public void Configure(EntityTypeBuilder<EvidenceTag> builder)
    {
        builder.ToTable("evidence_tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ModuleName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RecordId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RecordLabel).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NaacCriterion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.NaacIndicator).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => new { x.TenantId, x.NaacCriterion });
        builder.HasIndex(x => new { x.TenantId, x.ModuleName });
    }
}

public class EvidenceSummaryConfiguration : IEntityTypeConfiguration<EvidenceSummary>
{
    public void Configure(EntityTypeBuilder<EvidenceSummary> builder)
    {
        builder.ToTable("evidence_summaries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MetricKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NumericValue).HasColumnType("decimal(18,4)");
        builder.Property(x => x.TextValue).HasMaxLength(2000);
        builder.HasIndex(x => new { x.TenantId, x.AcademicYear, x.Module, x.Category, x.MetricKey }).IsUnique();
    }
}
