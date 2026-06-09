using ERP.Transport.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Transport.Infrastructure;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("transport_routes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.TotalStops).HasDefaultValue(0);
        builder.Property(x => x.TotalPassengers).HasDefaultValue(0);
        builder.HasMany(x => x.Stops).WithOne(x => x.Route).HasForeignKey(x => x.RouteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("route_stops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DistanceFromCollegeKm).HasColumnType("decimal(10,3)");
        builder.HasIndex(x => new { x.TenantId, x.RouteId, x.Sequence }).IsUnique();
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("transport_vehicles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RegistrationNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Make).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.TenantId, x.RegistrationNumber }).IsUnique();
    }
}

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("transport_drivers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LicenseNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.TenantId, x.LicenseNumber }).IsUnique();
    }
}

public class RouteAssignmentConfiguration : IEntityTypeConfiguration<RouteAssignment>
{
    public void Configure(EntityTypeBuilder<RouteAssignment> builder)
    {
        builder.ToTable("route_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MemberType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.MemberName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.TenantId, x.RouteId, x.MemberId, x.AcademicYear }).IsUnique();
    }
}

public class GpsLocationConfiguration : IEntityTypeConfiguration<GpsLocation>
{
    public void Configure(EntityTypeBuilder<GpsLocation> builder)
    {
        builder.ToTable("gps_locations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Latitude).HasColumnType("decimal(10,7)");
        builder.Property(x => x.Longitude).HasColumnType("decimal(10,7)");
        builder.Property(x => x.Speed).HasColumnType("decimal(10,2)");
        builder.Property(x => x.Heading).HasColumnType("decimal(10,2)");
        builder.Property(x => x.ProviderReference).HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.RecordedAt });
    }
}
