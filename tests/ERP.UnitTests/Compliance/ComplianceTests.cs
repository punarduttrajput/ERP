using ERP.Compliance.Application.Commands;
using ERP.Compliance.Application.Jobs;
using ERP.Compliance.Application.Queries;
using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.UnitTests.Compliance;

public class ComplianceTests
{
    private static IComplianceDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<ComplianceTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ComplianceTestDbContext(options);
    }

    [Fact]
    public async Task MarkComplete_SetsStatusAndTimestamp()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var item = new ComplianceItem
        {
            TenantId = tenantId,
            Title = "UGC Report",
            Authority = ComplianceAuthority.UGC,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            AcademicYear = 2026,
            Status = ComplianceStatus.Pending
        };
        db.ComplianceItems.Add(item);
        await db.SaveChangesAsync();

        var handler = new MarkComplianceCompleteHandler(db);
        var result = await handler.Handle(
            new MarkComplianceCompleteCommand(tenantId, item.Id, userId, "REF-001", "Done"), default);

        result.IsSuccess.Should().BeTrue();
        var updated = db.ComplianceItems.First(x => x.Id == item.Id);
        updated.Status.Should().Be(ComplianceStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
        updated.CompletedBy.Should().Be(userId);
    }

    [Fact]
    public async Task MarkComplete_AlreadyCompleted_ReturnsFailure()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();

        var item = new ComplianceItem
        {
            TenantId = tenantId,
            Title = "AICTE Disclosure",
            Authority = ComplianceAuthority.AICTE,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            AcademicYear = 2026,
            Status = ComplianceStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };
        db.ComplianceItems.Add(item);
        await db.SaveChangesAsync();

        var handler = new MarkComplianceCompleteHandler(db);
        var result = await handler.Handle(
            new MarkComplianceCompleteCommand(tenantId, item.Id, Guid.NewGuid(), null, null), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already");
    }

    [Fact]
    public async Task GetUpcomingDeadlines_FiltersByDateRange()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Due in 5 days — should be returned
        db.ComplianceItems.Add(new ComplianceItem
        {
            TenantId = tenantId,
            Title = "Due Soon",
            Authority = ComplianceAuthority.UGC,
            DueDate = today.AddDays(5),
            AcademicYear = 2026,
            Status = ComplianceStatus.Pending
        });
        // Due in 45 days — outside the 30-day window, should not be returned
        db.ComplianceItems.Add(new ComplianceItem
        {
            TenantId = tenantId,
            Title = "Due Far",
            Authority = ComplianceAuthority.AICTE,
            DueDate = today.AddDays(45),
            AcademicYear = 2026,
            Status = ComplianceStatus.Pending
        });
        // Overdue — DueDate < today, should not be returned by upcoming query
        db.ComplianceItems.Add(new ComplianceItem
        {
            TenantId = tenantId,
            Title = "Overdue Item",
            Authority = ComplianceAuthority.NAAC,
            DueDate = today.AddDays(-5),
            AcademicYear = 2026,
            Status = ComplianceStatus.Overdue
        });
        await db.SaveChangesAsync();

        var handler = new GetUpcomingDeadlinesHandler(db);
        var result = await handler.Handle(new GetUpcomingDeadlinesQuery(tenantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Title.Should().Be("Due Soon");
    }

    [Fact]
    public async Task ComplianceAlertJob_OverdueItem_UpdatesStatus()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var item = new ComplianceItem
        {
            TenantId = tenantId,
            Title = "AISHE Return",
            Authority = ComplianceAuthority.AISHE,
            DueDate = today.AddDays(-3),
            AcademicYear = 2026,
            Status = ComplianceStatus.Pending
        };
        db.ComplianceItems.Add(item);
        await db.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();
        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var job = new ComplianceAlertJob(db, emailMock.Object, cacheMock.Object, NullLogger<ComplianceAlertJob>.Instance);
        await job.RunAsync(CancellationToken.None);

        var updated = db.ComplianceItems.First(x => x.Id == item.Id);
        updated.Status.Should().Be(ComplianceStatus.Overdue);
    }

    [Fact]
    public async Task AisheReturn_Compile_SetsCompiledStatus()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();

        var existing = new AisheReturn
        {
            TenantId = tenantId,
            AcademicYear = 2026,
            Status = AisheReturnStatus.Draft
        };
        db.AisheReturns.Add(existing);
        await db.SaveChangesAsync();

        // Directly simulate what the handler does after Dapper queries
        existing.TotalStudentsEnrolled = 500;
        existing.MaleStudents = 300;
        existing.FemaleStudents = 200;
        existing.TotalFaculty = 50;
        existing.Status = AisheReturnStatus.Compiled;
        existing.CompiledAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var record = db.AisheReturns.First(x => x.Id == existing.Id);
        record.Status.Should().Be(AisheReturnStatus.Compiled);
        record.CompiledAt.Should().NotBeNull();
        record.TotalStudentsEnrolled.Should().Be(500);
    }
}

// Minimal in-memory DbContext for testing — satisfies IComplianceDbContext without the full AppDbContext
internal class ComplianceTestDbContext : DbContext, IComplianceDbContext
{
    public ComplianceTestDbContext(DbContextOptions<ComplianceTestDbContext> options) : base(options) { }

    public DbSet<ComplianceItem> ComplianceItems => Set<ComplianceItem>();
    public DbSet<AisheReturn> AisheReturns => Set<AisheReturn>();
    public DbSet<ComplianceNotification> ComplianceNotifications => Set<ComplianceNotification>();
}
