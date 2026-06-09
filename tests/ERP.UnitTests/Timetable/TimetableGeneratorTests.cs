using ERP.Shared.Domain;
using ERP.Timetable.Application.Services;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Timetable;

public class TimetableGeneratorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();
    private static readonly Guid BatchId = Guid.NewGuid();

    private static List<TimeSlot> MakeSlots(int count)
    {
        var slots = new List<TimeSlot>();
        int day = 1;
        int period = 1;
        for (int i = 0; i < count; i++)
        {
            slots.Add(new TimeSlot
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                DayOfWeek = day,
                PeriodNumber = period,
                StartTime = new TimeOnly(8 + i, 0),
                EndTime = new TimeOnly(9 + i, 0),
                IsBreak = false
            });
            period++;
            if (period > 6) { period = 1; day++; }
        }
        return slots;
    }

    private static ITimetableDbContext BuildContext(
        List<FacultySubjectAssignment> assignments,
        List<TimeSlot> slots,
        List<TimetableEntry> existingEntries,
        List<FacultyWorkload> workloads,
        List<Room> rooms)
    {
        var options = new DbContextOptionsBuilder<TestTimetableDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new TestTimetableDbContext(options);
        ctx.FacultySubjectAssignments.AddRange(assignments);
        ctx.TimeSlots.AddRange(slots);
        ctx.TimetableEntries.AddRange(existingEntries);
        ctx.FacultyWorkloads.AddRange(workloads);
        ctx.Rooms.AddRange(rooms);
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task No_Conflicts_Generated_For_Valid_Input()
    {
        var faculty1 = Guid.NewGuid();
        var faculty2 = Guid.NewGuid();
        var subject1 = Guid.NewGuid();
        var subject2 = Guid.NewGuid();
        var room1 = new Room { Id = Guid.NewGuid(), TenantId = TenantId, Code = "R1", Name = "Room 1", Capacity = 40, RoomType = "Lecture", IsActive = true };
        var room2 = new Room { Id = Guid.NewGuid(), TenantId = TenantId, Code = "R2", Name = "Room 2", Capacity = 40, RoomType = "Lecture", IsActive = true };
        var slots = MakeSlots(10);
        var assignments = new List<FacultySubjectAssignment>
        {
            new() { Id = Guid.NewGuid(), TenantId = TenantId, FacultyUserId = faculty1, SubjectId = subject1, SemesterId = SemesterId, BatchId = BatchId, HoursPerWeek = 3 },
            new() { Id = Guid.NewGuid(), TenantId = TenantId, FacultyUserId = faculty2, SubjectId = subject2, SemesterId = SemesterId, BatchId = BatchId, HoursPerWeek = 3 }
        };

        var ctx = BuildContext(assignments, slots, new List<TimetableEntry>(), new List<FacultyWorkload>(), new List<Room> { room1, room2 });
        var service = new TimetableGeneratorService(ctx);

        var result = await service.GenerateAsync(SemesterId, BatchId, TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(6);

        var entries = ((TestTimetableDbContext)ctx).TimetableEntries.ToList();
        // No two entries share same batch+slot
        entries.GroupBy(e => e.TimeSlotId).Any(g => g.Count() > 1).Should().BeFalse();
        // No faculty double-booking
        entries.GroupBy(e => (e.FacultyUserId, e.TimeSlotId)).Any(g => g.Count() > 1).Should().BeFalse();
        // No room double-booking
        entries.GroupBy(e => (e.RoomId, e.TimeSlotId)).Any(g => g.Count() > 1).Should().BeFalse();
    }

    [Fact]
    public async Task Faculty_Double_Booking_Prevented()
    {
        // Same faculty assigned to two subjects in different batches — generator must use different slots
        var faculty = Guid.NewGuid();
        var subject1 = Guid.NewGuid();
        var subject2 = Guid.NewGuid();
        var batch2 = Guid.NewGuid();
        var room = new Room { Id = Guid.NewGuid(), TenantId = TenantId, Code = "R1", Name = "Room 1", Capacity = 40, RoomType = "Lecture", IsActive = true };
        var slots = MakeSlots(10);

        // Pre-book batch2 into slot[0] for the faculty (simulates another batch already scheduled)
        var existingEntry = new TimetableEntry
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            SemesterId = SemesterId,
            BatchId = batch2,
            SubjectId = subject2,
            FacultyUserId = faculty,
            RoomId = room.Id,
            TimeSlotId = slots[0].Id,
            Status = TimetableStatus.Published
        };

        var assignments = new List<FacultySubjectAssignment>
        {
            new() { Id = Guid.NewGuid(), TenantId = TenantId, FacultyUserId = faculty, SubjectId = subject1, SemesterId = SemesterId, BatchId = BatchId, HoursPerWeek = 2 }
        };

        var ctx = BuildContext(assignments, slots, new List<TimetableEntry> { existingEntry }, new List<FacultyWorkload>(), new List<Room> { room });
        var service = new TimetableGeneratorService(ctx);

        var result = await service.GenerateAsync(SemesterId, BatchId, TenantId);

        result.IsSuccess.Should().BeTrue();

        var newEntries = ((TestTimetableDbContext)ctx).TimetableEntries
            .Where(e => e.BatchId == BatchId)
            .ToList();

        // None of the new entries should use slot[0] because faculty is already booked there
        newEntries.Any(e => e.TimeSlotId == slots[0].Id).Should().BeFalse();
    }

    [Fact]
    public async Task Returns_Failure_When_No_Slots_Available()
    {
        var faculty = Guid.NewGuid();
        var subject = Guid.NewGuid();
        var room = new Room { Id = Guid.NewGuid(), TenantId = TenantId, Code = "R1", Name = "Room 1", Capacity = 40, RoomType = "Lecture", IsActive = true };
        // Only 3 slots available but subject needs 5 hours/week
        var slots = MakeSlots(3);

        var assignments = new List<FacultySubjectAssignment>
        {
            new() { Id = Guid.NewGuid(), TenantId = TenantId, FacultyUserId = faculty, SubjectId = subject, SemesterId = SemesterId, BatchId = BatchId, HoursPerWeek = 5 }
        };

        var ctx = BuildContext(assignments, slots, new List<TimetableEntry>(), new List<FacultyWorkload>(), new List<Room> { room });
        var service = new TimetableGeneratorService(ctx);

        var result = await service.GenerateAsync(SemesterId, BatchId, TenantId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(subject.ToString());
    }

    [Fact]
    public async Task Workload_Limit_Respected()
    {
        var faculty = Guid.NewGuid();
        var subject = Guid.NewGuid();
        var room = new Room { Id = Guid.NewGuid(), TenantId = TenantId, Code = "R1", Name = "Room 1", Capacity = 40, RoomType = "Lecture", IsActive = true };
        var slots = MakeSlots(10);

        // Faculty max is 2 hours but assignment needs 3
        var workload = new FacultyWorkload
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            FacultyUserId = faculty,
            SemesterId = SemesterId,
            MaxHoursPerWeek = 2,
            AssignedHoursPerWeek = 0
        };

        var assignments = new List<FacultySubjectAssignment>
        {
            new() { Id = Guid.NewGuid(), TenantId = TenantId, FacultyUserId = faculty, SubjectId = subject, SemesterId = SemesterId, BatchId = BatchId, HoursPerWeek = 3 }
        };

        var ctx = BuildContext(assignments, slots, new List<TimetableEntry>(), new List<FacultyWorkload> { workload }, new List<Room> { room });
        var service = new TimetableGeneratorService(ctx);

        var result = await service.GenerateAsync(SemesterId, BatchId, TenantId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(subject.ToString());
    }
}

// Minimal in-memory DbContext implementing ITimetableDbContext for tests
internal class TestTimetableDbContext : DbContext, ITimetableDbContext
{
    public TestTimetableDbContext(DbContextOptions<TestTimetableDbContext> options) : base(options) { }

    public DbSet<ERP.Timetable.Domain.Room> Rooms => Set<ERP.Timetable.Domain.Room>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<FacultyWorkload> FacultyWorkloads => Set<FacultyWorkload>();
    public DbSet<FacultySubjectAssignment> FacultySubjectAssignments => Set<FacultySubjectAssignment>();
    public DbSet<SubstituteAssignment> SubstituteAssignments => Set<SubstituteAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Disable global query filters for tests — no tenant context available
        modelBuilder.Entity<TimetableEntry>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FacultySubjectAssignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TimeSlot>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FacultyWorkload>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ERP.Timetable.Domain.Room>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SubstituteAssignment>().HasQueryFilter(e => !e.IsDeleted);
    }
}
