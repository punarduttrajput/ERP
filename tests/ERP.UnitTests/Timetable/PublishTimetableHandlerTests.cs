using ERP.Shared.Application.Abstractions;
using ERP.Timetable.API.Hubs;
using ERP.Timetable.Application.Commands;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Timetable;

public class PublishTimetableHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();
    private static readonly Guid BatchId = Guid.NewGuid();

    private static TestTimetableDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTimetableDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestTimetableDbContext(options);
    }

    private static (PublishTimetableHandler handler, TestTimetableDbContext ctx) BuildHandler()
    {
        var ctx = CreateContext();

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.Setup(x => x.TenantId).Returns(TenantId);

        var hubClients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        hubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<TimetableHub>>();
        hubContext.Setup(x => x.Clients).Returns(hubClients.Object);

        var handler = new PublishTimetableHandler(ctx, currentTenant.Object, hubContext.Object);
        return (handler, ctx);
    }

    private static TimetableEntry MakeEntry(TimetableStatus status) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
        SemesterId = SemesterId,
        BatchId = BatchId,
        SubjectId = Guid.NewGuid(),
        FacultyUserId = Guid.NewGuid(),
        RoomId = Guid.NewGuid(),
        TimeSlotId = Guid.NewGuid(),
        Status = status
    };

    [Fact]
    public async Task Publishing_Draft_Entries_Sets_Status_To_Published()
    {
        var (handler, ctx) = BuildHandler();

        var entries = new[] { MakeEntry(TimetableStatus.Draft), MakeEntry(TimetableStatus.Draft), MakeEntry(TimetableStatus.Draft) };
        ctx.TimetableEntries.AddRange(entries);
        await ctx.SaveChangesAsync();

        var result = await handler.Handle(new PublishTimetableCommand(SemesterId, BatchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var stored = ctx.TimetableEntries.ToList();
        stored.Should().AllSatisfy(e => e.Status.Should().Be(TimetableStatus.Published));
    }

    [Fact]
    public async Task Publishing_Already_Published_Timetable_Returns_Success()
    {
        var (handler, ctx) = BuildHandler();

        // Already published entries — no drafts
        var entries = new[] { MakeEntry(TimetableStatus.Published), MakeEntry(TimetableStatus.Published) };
        ctx.TimetableEntries.AddRange(entries);
        await ctx.SaveChangesAsync();

        var result = await handler.Handle(new PublishTimetableCommand(SemesterId, BatchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
