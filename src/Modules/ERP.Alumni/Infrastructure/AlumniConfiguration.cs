using ERP.Alumni.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Alumni.Infrastructure;

public class AlumniProfileConfiguration : IEntityTypeConfiguration<AlumniProfile>
{
    public void Configure(EntityTypeBuilder<AlumniProfile> builder)
    {
        builder.ToTable("alumni_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(20);
        builder.Property(x => x.ProgramName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BatchName).HasMaxLength(100);
        builder.Property(x => x.CurrentEmployer).HasMaxLength(300);
        builder.Property(x => x.CurrentJobTitle).HasMaxLength(200);
        builder.Property(x => x.CurrentCity).HasMaxLength(100);
        builder.Property(x => x.CurrentCountry).HasMaxLength(100).HasDefaultValue("India").IsRequired();
        builder.Property(x => x.LinkedInUrl).HasMaxLength(500);
        builder.Property(x => x.AvatarUrl).HasMaxLength(1000);
        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
    }
}

public class AlumniEventConfiguration : IEntityTypeConfiguration<AlumniEvent>
{
    public void Configure(EntityTypeBuilder<AlumniEvent> builder)
    {
        builder.ToTable("alumni_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(5000);
        builder.Property(x => x.EventType).HasConversion<int>();
        builder.Property(x => x.VenueOrLink).HasMaxLength(500);
        builder.HasMany(x => x.Registrations).WithOne(x => x.Event).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.TenantId, x.EventDate });
        builder.HasIndex(x => new { x.TenantId, x.IsPublished });
    }
}

public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.ToTable("event_registrations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.EventId, x.AlumniId }).IsUnique();
    }
}

public class DonationCampaignConfiguration : IEntityTypeConfiguration<DonationCampaign>
{
    public void Configure(EntityTypeBuilder<DonationCampaign> builder)
    {
        builder.ToTable("donation_campaigns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(5000);
        builder.Property(x => x.TargetAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CollectedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Section80GRegistrationNumber).HasMaxLength(100);
        builder.HasMany(x => x.Pledges).WithOne(x => x.Campaign).HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

public class DonationPledgeConfiguration : IEntityTypeConfiguration<DonationPledge>
{
    public void Configure(EntityTypeBuilder<DonationPledge> builder)
    {
        builder.ToTable("donation_pledges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AlumniName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AlumniEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.PledgedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ReceiptNumber).HasMaxLength(50);
        builder.HasIndex(x => new { x.TenantId, x.CampaignId });
        builder.HasIndex(x => new { x.TenantId, x.AlumniId });
    }
}
