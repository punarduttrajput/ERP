using ERP.Placement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Placement.Infrastructure;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("placement_companies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Industry).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Website).HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.LogoUrl).HasMaxLength(1000);
        builder.Property(x => x.ContactPersonName).HasMaxLength(200);
        builder.Property(x => x.ContactEmail).HasMaxLength(320);
        builder.Property(x => x.ContactMobile).HasMaxLength(20);
        builder.Property(x => x.TotalDrives).HasDefaultValue(0);
        builder.Property(x => x.TotalOffers).HasDefaultValue(0);
        builder.Property(x => x.HighestPackageLpa).HasColumnType("decimal(10,2)").HasDefaultValue(0);
        builder.Property(x => x.AveragePackageLpa).HasColumnType("decimal(10,2)").HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

public class PlacementDriveConfiguration : IEntityTypeConfiguration<PlacementDrive>
{
    public void Configure(EntityTypeBuilder<PlacementDrive> builder)
    {
        builder.ToTable("placement_drives");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.JobRole).HasMaxLength(200).IsRequired();
        builder.Property(x => x.JobDescription).HasMaxLength(5000);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.PackageLpa).HasColumnType("decimal(10,2)");
        builder.Property(x => x.MinCgpa).HasColumnType("decimal(4,2)").HasDefaultValue(0);
        builder.Property(x => x.MaxBacklogs).HasDefaultValue(0);
        builder.Property(x => x.EligibleBranches).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.TotalRegistrations).HasDefaultValue(0);
        builder.Property(x => x.TotalSelected).HasDefaultValue(0);
        builder.HasOne(x => x.Company)
            .WithMany(x => x.Drives)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.CompanyId });
        builder.HasIndex(x => new { x.TenantId, x.AcademicYear });
    }
}

public class DriveRegistrationConfiguration : IEntityTypeConfiguration<DriveRegistration>
{
    public void Configure(EntityTypeBuilder<DriveRegistration> builder)
    {
        builder.ToTable("drive_registrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Branch).HasMaxLength(50).IsRequired();
        builder.Property(x => x.StudentCgpa).HasColumnType("decimal(4,2)");
        builder.Property(x => x.InterviewNotes).HasMaxLength(2000);
        builder.Property(x => x.OfferLpa).HasColumnType("decimal(10,2)");
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasOne(x => x.Drive)
            .WithMany(x => x.Registrations)
            .HasForeignKey(x => x.DriveId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.DriveId, x.StudentId }).IsUnique();
    }
}

public class PlacementOfferConfiguration : IEntityTypeConfiguration<PlacementOffer>
{
    public void Configure(EntityTypeBuilder<PlacementOffer> builder)
    {
        builder.ToTable("placement_offers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.JobRole).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OfferedPackageLpa).HasColumnType("decimal(10,2)");
        builder.Property(x => x.OfferLetterBlobUrl).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasOne(x => x.Registration)
            .WithOne(x => x.Offer)
            .HasForeignKey<PlacementOffer>(x => x.RegistrationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.RegistrationId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.DriveId });
        builder.HasIndex(x => new { x.TenantId, x.StudentId });
    }
}
