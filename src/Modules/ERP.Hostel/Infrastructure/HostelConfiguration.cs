using ERP.Hostel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Hostel.Infrastructure;

public class HostelBlockConfiguration : IEntityTypeConfiguration<HostelBlock>
{
    public void Configure(EntityTypeBuilder<HostelBlock> builder)
    {
        builder.ToTable("hostel_blocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Gender).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TotalRooms).HasDefaultValue(0);
        builder.Property(x => x.OccupiedRooms).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasMany(x => x.Rooms).WithOne(x => x.Block).HasForeignKey(x => x.BlockId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

public class HostelRoomConfiguration : IEntityTypeConfiguration<HostelRoom>
{
    public void Configure(EntityTypeBuilder<HostelRoom> builder)
    {
        builder.ToTable("hostel_rooms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoomNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RoomType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.OccupiedCount).HasDefaultValue(0);
        builder.Property(x => x.MonthlyRent).HasColumnType("decimal(18,2)");
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.TenantId, x.BlockId, x.RoomNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.BlockId });
    }
}

public class RoomAllocationConfiguration : IEntityTypeConfiguration<RoomAllocation>
{
    public void Configure(EntityTypeBuilder<RoomAllocation> builder)
    {
        builder.ToTable("room_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasOne(x => x.Room).WithMany(x => x.Allocations).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.RoomId, x.StudentId, x.AcademicYear });
        builder.HasIndex(x => new { x.TenantId, x.StudentId });
    }
}

public class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.ToTable("hostel_waitlist");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PreferredRoomType).HasConversion<int>();
        builder.Property(x => x.IsPromoted).HasDefaultValue(false);
        builder.HasIndex(x => new { x.TenantId, x.StudentId, x.AcademicYear });
        builder.HasIndex(x => new { x.TenantId, x.Priority });
    }
}

public class VisitorEntryConfiguration : IEntityTypeConfiguration<VisitorEntry>
{
    public void Configure(EntityTypeBuilder<VisitorEntry> builder)
    {
        builder.ToTable("visitor_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VisitorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.VisitorMobile).HasMaxLength(20).IsRequired();
        builder.Property(x => x.VisitorIdType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.VisitorIdNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PurposeOfVisit).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.BlockId });
        builder.HasIndex(x => new { x.TenantId, x.StudentId });
    }
}
