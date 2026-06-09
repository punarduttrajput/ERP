using ERP.NAAC.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.NAAC.Infrastructure;

public class SsrReportConfiguration : IEntityTypeConfiguration<SsrReport>
{
    public void Configure(EntityTypeBuilder<SsrReport> builder)
    {
        builder.ToTable("ssr_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.AcademicYear }).IsUnique();
        builder.HasMany(x => x.Sections)
               .WithOne(x => x.Ssr)
               .HasForeignKey(x => x.SsrId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SsrSectionConfiguration : IEntityTypeConfiguration<SsrSection>
{
    public void Configure(EntityTypeBuilder<SsrSection> builder)
    {
        builder.ToTable("ssr_sections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CriterionNumber).HasMaxLength(5).IsRequired();
        builder.Property(x => x.IndicatorNumber).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        // longtext required: max 50000 chars exceeds varchar(65535) boundary on some MySQL configs
        builder.Property(x => x.Content).HasColumnType("longtext").IsRequired();
        builder.Property(x => x.AutoMetrics).HasMaxLength(5000);
        builder.HasIndex(x => new { x.TenantId, x.SsrId, x.IndicatorNumber });
    }
}

public class DvvQueryConfiguration : IEntityTypeConfiguration<DvvQuery>
{
    public void Configure(EntityTypeBuilder<DvvQuery> builder)
    {
        builder.ToTable("dvv_queries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QueryNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CriterionNumber).HasMaxLength(5).IsRequired();
        builder.Property(x => x.IndicatorNumber).HasMaxLength(10).IsRequired();
        builder.Property(x => x.QueryText).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.Response).HasMaxLength(10000);
        builder.Property(x => x.SupportingDocUrls).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.SsrId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public class AqarReportConfiguration : IEntityTypeConfiguration<AqarReport>
{
    public void Configure(EntityTypeBuilder<AqarReport> builder)
    {
        builder.ToTable("aqar_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.AcademicYear }).IsUnique();
        builder.HasMany(x => x.Sections)
               .WithOne(x => x.Aqar)
               .HasForeignKey(x => x.AqarId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AqarSectionConfiguration : IEntityTypeConfiguration<AqarSection>
{
    public void Configure(EntityTypeBuilder<AqarSection> builder)
    {
        builder.ToTable("aqar_sections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CriterionNumber).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Content).HasColumnType("longtext");
        builder.Property(x => x.ReviewComment).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.AqarId });
    }
}
