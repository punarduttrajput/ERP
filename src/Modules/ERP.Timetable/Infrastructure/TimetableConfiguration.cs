using ERP.Timetable.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Timetable.Infrastructure;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RoomType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Building).HasMaxLength(100);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.ToTable("time_slots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IsBreak).HasDefaultValue(false);
        builder.HasIndex(x => new { x.TenantId, x.DayOfWeek, x.PeriodNumber }).IsUnique();
    }
}

public class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> builder)
    {
        builder.ToTable("timetable_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.IsSubstitute).HasDefaultValue(false);

        builder.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TimeSlot).WithMany().HasForeignKey(x => x.TimeSlotId).OnDelete(DeleteBehavior.Restrict);

        // One class per batch per slot
        builder.HasIndex(x => new { x.TenantId, x.SemesterId, x.BatchId, x.TimeSlotId }).IsUnique()
            .HasDatabaseName("IX_timetable_entries_batch_slot");

        // No faculty double-booking
        builder.HasIndex(x => new { x.TenantId, x.SemesterId, x.FacultyUserId, x.TimeSlotId }).IsUnique()
            .HasDatabaseName("IX_timetable_entries_faculty_slot");

        // No room double-booking
        builder.HasIndex(x => new { x.TenantId, x.SemesterId, x.RoomId, x.TimeSlotId }).IsUnique()
            .HasDatabaseName("IX_timetable_entries_room_slot");
    }
}

public class FacultyWorkloadConfiguration : IEntityTypeConfiguration<FacultyWorkload>
{
    public void Configure(EntityTypeBuilder<FacultyWorkload> builder)
    {
        builder.ToTable("faculty_workloads");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AssignedHoursPerWeek).HasDefaultValue(0);
        builder.HasIndex(x => new { x.TenantId, x.FacultyUserId, x.SemesterId }).IsUnique();
    }
}

public class FacultySubjectAssignmentConfiguration : IEntityTypeConfiguration<FacultySubjectAssignment>
{
    public void Configure(EntityTypeBuilder<FacultySubjectAssignment> builder)
    {
        builder.ToTable("faculty_subject_assignments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.FacultyUserId, x.SubjectId, x.SemesterId, x.BatchId }).IsUnique();
    }
}

public class SubstituteAssignmentConfiguration : IEntityTypeConfiguration<SubstituteAssignment>
{
    public void Configure(EntityTypeBuilder<SubstituteAssignment> builder)
    {
        builder.ToTable("substitute_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500);
    }
}
