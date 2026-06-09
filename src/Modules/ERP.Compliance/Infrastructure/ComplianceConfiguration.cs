using ERP.Compliance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Compliance.Infrastructure;

public class ComplianceItemConfiguration : IEntityTypeConfiguration<ComplianceItem>
{
    public void Configure(EntityTypeBuilder<ComplianceItem> builder)
    {
        builder.ToTable("compliance_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Authority).HasConversion<int>();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.ResponsiblePersonName).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.SubmissionReference).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.RecurrencePattern).HasMaxLength(50);
        builder.HasIndex(x => new { x.TenantId, x.Authority, x.AcademicYear });
        builder.HasIndex(x => new { x.TenantId, x.DueDate });
    }
}

public class AisheReturnConfiguration : IEntityTypeConfiguration<AisheReturn>
{
    public void Configure(EntityTypeBuilder<AisheReturn> builder)
    {
        builder.ToTable("aishe_returns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.InstitutionType).HasMaxLength(100);
        builder.Property(x => x.TotalBuiltAreaSqm).HasColumnType("decimal(12,2)");
        builder.Property(x => x.SubmissionReference).HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.AcademicYear }).IsUnique();
    }
}

public class ComplianceNotificationConfiguration : IEntityTypeConfiguration<ComplianceNotification>
{
    public void Configure(EntityTypeBuilder<ComplianceNotification> builder)
    {
        builder.ToTable("compliance_notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Message).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NotificationType).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.RecipientUserId, x.IsRead });
        builder.HasIndex(x => new { x.TenantId, x.ComplianceItemId });
    }
}
