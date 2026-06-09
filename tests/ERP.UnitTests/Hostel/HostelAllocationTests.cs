using ERP.Hostel.Application.Commands;
using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ERP.UnitTests.Hostel;

// Minimal EF Core in-memory context implementing IHostelDbContext.
// A real AppDbContext cannot be used here because the test project does not reference ERP.Host.
internal class TestHostelDbContext : DbContext, IHostelDbContext
{
    public TestHostelDbContext(DbContextOptions options) : base(options) { }

    public DbSet<HostelBlock> HostelBlocks => Set<HostelBlock>();
    public DbSet<HostelRoom> HostelRooms => Set<HostelRoom>();
    public DbSet<RoomAllocation> RoomAllocations => Set<RoomAllocation>();
    public DbSet<WaitlistEntry> HostelWaitlist => Set<WaitlistEntry>();
    public DbSet<VisitorEntry> VisitorEntries => Set<VisitorEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<HostelRoom>()
            .HasOne(r => r.Block).WithMany(b => b.Rooms).HasForeignKey(r => r.BlockId);
        modelBuilder.Entity<RoomAllocation>()
            .HasOne(a => a.Room).WithMany(r => r.Allocations).HasForeignKey(a => a.RoomId);
    }
}

public class HostelAllocationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static TestHostelDbContext BuildDb() =>
        new(new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentTenant MockTenant()
    {
        var mock = new Mock<ICurrentTenant>();
        mock.Setup(t => t.TenantId).Returns(TenantId);
        return mock.Object;
    }

    private static (HostelBlock block, HostelRoom room) SeedRoom(TestHostelDbContext db, int capacity, int occupiedCount)
    {
        var block = new HostelBlock
        {
            TenantId = TenantId,
            Name = "Block A",
            Gender = "Male"
        };
        db.HostelBlocks.Add(block);

        var room = new HostelRoom
        {
            TenantId = TenantId,
            BlockId = block.Id,
            RoomNumber = "101",
            Floor = 1,
            RoomType = RoomType.Double,
            Capacity = capacity,
            OccupiedCount = occupiedCount,
            MonthlyRent = 3000,
            Status = occupiedCount == 0 ? RoomStatus.Available
                   : occupiedCount >= capacity ? RoomStatus.FullyOccupied
                   : RoomStatus.PartiallyOccupied,
            Block = block
        };
        db.HostelRooms.Add(room);
        db.SaveChanges();
        return (block, room);
    }

    [Fact]
    public async Task AllocateRoom_WhenAvailable_SetsOccupied()
    {
        using var db = BuildDb();
        var (_, room) = SeedRoom(db, capacity: 2, occupiedCount: 0);
        var handler = new AllocateRoomCommandHandler(db, MockTenant());

        var result = await handler.Handle(
            new AllocateRoomCommand(room.Id, Guid.NewGuid(), "Student A", 2024),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsWaitlisted.Should().BeFalse();

        var updated = await db.HostelRooms.FindAsync(room.Id);
        updated!.OccupiedCount.Should().Be(1);
        updated.Status.Should().Be(RoomStatus.PartiallyOccupied);

        (await db.RoomAllocations.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AllocateRoom_WhenFull_AddsToWaitlist()
    {
        using var db = BuildDb();
        var (_, room) = SeedRoom(db, capacity: 2, occupiedCount: 2);
        var handler = new AllocateRoomCommandHandler(db, MockTenant());

        var result = await handler.Handle(
            new AllocateRoomCommand(room.Id, Guid.NewGuid(), "Student B", 2024),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsWaitlisted.Should().BeTrue();
        result.Value.Message.Should().Contain("waitlist");

        (await db.RoomAllocations.CountAsync()).Should().Be(0);
        (await db.HostelWaitlist.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AllocateRoom_FillsToCapacity_SetsFullyOccupied()
    {
        using var db = BuildDb();
        var (block, room) = SeedRoom(db, capacity: 1, occupiedCount: 0);
        var handler = new AllocateRoomCommandHandler(db, MockTenant());

        var result = await handler.Handle(
            new AllocateRoomCommand(room.Id, Guid.NewGuid(), "Student C", 2024),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.HostelRooms.FindAsync(room.Id);
        updated!.OccupiedCount.Should().Be(1);
        updated.Status.Should().Be(RoomStatus.FullyOccupied);

        var updatedBlock = await db.HostelBlocks.FindAsync(block.Id);
        updatedBlock!.OccupiedRooms.Should().Be(1);
    }

    [Fact]
    public async Task DeallocateRoom_PromotesFromWaitlist()
    {
        using var db = BuildDb();
        var (_, room) = SeedRoom(db, capacity: 1, occupiedCount: 0);

        // Allocate directly to make the room full
        var allocation = new RoomAllocation
        {
            TenantId = TenantId,
            RoomId = room.Id,
            StudentId = Guid.NewGuid(),
            StudentName = "Student D",
            AcademicYear = 2024,
            AllocatedAt = DateTime.UtcNow,
            Status = AllocationStatus.Active
        };
        db.RoomAllocations.Add(allocation);
        room.OccupiedCount = 1;
        room.Status = RoomStatus.FullyOccupied;
        await db.SaveChangesAsync();

        // Add a waitlist entry for the same block and room type
        var waitlistStudent = Guid.NewGuid();
        db.HostelWaitlist.Add(new WaitlistEntry
        {
            TenantId = TenantId,
            StudentId = waitlistStudent,
            StudentName = "Waitlisted Student",
            PreferredRoomType = RoomType.Double,
            PreferredBlockId = room.BlockId,
            AcademicYear = 2024,
            RequestedAt = DateTime.UtcNow,
            Priority = 1,
            IsPromoted = false
        });
        await db.SaveChangesAsync();

        var mediatorMock = new Mock<IMediator>();
        // When the mediator is asked to allocate, run the real handler inline
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AllocateRoomCommand>(), It.IsAny<CancellationToken>()))
            .Returns<AllocateRoomCommand, CancellationToken>((cmd, ct) =>
                new AllocateRoomCommandHandler(db, MockTenant()).Handle(cmd, ct));

        var deallocateHandler = new DeallocateRoomCommandHandler(db, mediatorMock.Object);
        var result = await deallocateHandler.Handle(new DeallocateRoomCommand(allocation.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var promotedEntry = await db.HostelWaitlist.FirstAsync();
        promotedEntry.IsPromoted.Should().BeTrue();

        // Room should have been re-allocated to the waitlisted student
        var newAllocation = await db.RoomAllocations
            .FirstOrDefaultAsync(a => a.StudentId == waitlistStudent && a.Status == AllocationStatus.Active);
        newAllocation.Should().NotBeNull();
    }

    [Fact]
    public async Task DeallocateRoom_NoWaitlist_RoomBecomesAvailable()
    {
        using var db = BuildDb();
        var (_, room) = SeedRoom(db, capacity: 2, occupiedCount: 1);
        room.Status = RoomStatus.PartiallyOccupied;

        var allocation = new RoomAllocation
        {
            TenantId = TenantId,
            RoomId = room.Id,
            StudentId = Guid.NewGuid(),
            StudentName = "Student E",
            AcademicYear = 2024,
            AllocatedAt = DateTime.UtcNow,
            Status = AllocationStatus.Active
        };
        db.RoomAllocations.Add(allocation);
        await db.SaveChangesAsync();

        var mediatorMock = new Mock<IMediator>();
        var handler = new DeallocateRoomCommandHandler(db, mediatorMock.Object);

        var result = await handler.Handle(new DeallocateRoomCommand(allocation.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.HostelRooms.FindAsync(room.Id);
        updated!.OccupiedCount.Should().Be(0);
        updated.Status.Should().Be(RoomStatus.Available);
    }

    [Fact]
    public async Task CheckIn_Visitor_RecordsEntry()
    {
        using var db = BuildDb();
        var handler = new CheckInVisitorCommandHandler(db, MockTenant());

        var result = await handler.Handle(new CheckInVisitorCommand(
            "Visitor One",
            "9876543210",
            "Aadhar",
            "1234-5678-9012",
            Guid.NewGuid(),
            "Student F",
            Guid.NewGuid(),
            "Family visit",
            Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var entry = await db.VisitorEntries.FindAsync(result.Value);
        entry.Should().NotBeNull();
        entry!.CheckOutAt.Should().BeNull();
        entry.VisitorName.Should().Be("Visitor One");
    }

    [Fact]
    public async Task CheckOut_Visitor_SetsCheckOutTime()
    {
        using var db = BuildDb();

        var entry = new VisitorEntry
        {
            TenantId = TenantId,
            VisitorName = "Visitor Two",
            VisitorMobile = "1234567890",
            VisitorIdType = "Passport",
            VisitorIdNumber = "P123456",
            StudentId = Guid.NewGuid(),
            StudentName = "Student G",
            BlockId = Guid.NewGuid(),
            PurposeOfVisit = "Delivery",
            CheckInAt = DateTime.UtcNow.AddHours(-1),
            CheckOutAt = null,
            CheckedInBy = Guid.NewGuid()
        };
        db.VisitorEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new CheckOutVisitorCommandHandler(db);
        var result = await handler.Handle(new CheckOutVisitorCommand(entry.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.VisitorEntries.FindAsync(entry.Id);
        updated!.CheckOutAt.Should().NotBeNull();
        updated.CheckOutAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
