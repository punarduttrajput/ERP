using ERP.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Tenants.Infrastructure;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.LogoUrl).HasMaxLength(500);
        builder.Property(t => t.PrimaryColor).HasMaxLength(20);
        builder.Property(t => t.SecondaryColor).HasMaxLength(20);
        builder.Property(t => t.CustomDomain).HasMaxLength(200);
        builder.HasIndex(t => t.CustomDomain).IsUnique().HasFilter("custom_domain IS NOT NULL");
        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.ContactEmail).HasMaxLength(200);
        builder.Property(t => t.ContactPhone).HasMaxLength(50);
        builder.Property(t => t.Address).HasMaxLength(500);
        builder.Property(t => t.Plan).HasMaxLength(50);
        builder.Property(t => t.SuspensionReason).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasOne(t => t.Settings)
            .WithOne(s => s.Tenant)
            .HasForeignKey<TenantSettings>(s => s.TenantId);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("tenant_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TimeZone).HasMaxLength(100);
        builder.Property(s => s.DateFormat).HasMaxLength(50);
        builder.Property(s => s.CurrencyCode).HasMaxLength(10);
        builder.Property(s => s.Language).HasMaxLength(20);
        builder.Property(s => s.CustomCss).HasColumnType("text");

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
