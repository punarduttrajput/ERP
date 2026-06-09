using ERP.Attendance.Application.Commands;
using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Attendance;

public class AttendanceTests
{
    private static IAttendanceDbContext BuildInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TestAttendanceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestAttendanceDbContext(options);
    }

    [Fact]
    public async Task CreateSession_PrePopulatesAbsentRecords()
    {
        var db = BuildInMemoryContext(nameof(CreateSession_PrePopulatesAbsentRecords));
        var handler = new CreateSessionHandler(db);
        var tenantId = Guid.NewGuid();
        var studentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var cmd = new CreateSessionCommand(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            1,
            studentIds);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var sessionId = result.Value;
        var records = await db.AttendanceRecords
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();

        records.Should().HaveCount(3);
        records.Should().AllSatisfy(r => r.Status.Should().Be(AttendanceStatus.Absent));
    }

    [Fact]
    public async Task MarkAttendance_LocksSessionAfterSubmit()
    {
        var db = BuildInMemoryContext(nameof(MarkAttendance_LocksSessionAfterSubmit));
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var session = new AttendanceSession
        {
            TenantId = tenantId,
            SubjectId = Guid.NewGuid(),
            BatchId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            FacultyUserId = Guid.NewGuid(),
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            PeriodNumber = 1
        };
        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync();

        var handler = new MarkAttendanceHandler(db);
        var result = await handler.Handle(new MarkAttendanceCommand(
            tenantId,
            session.Id,
            new[] { new AttendanceMark(studentId, AttendanceStatus.Present) }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var saved = await db.AttendanceSessions.FindAsync(session.Id);
        saved!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAttendance_LockedSession_ReturnsFailure()
    {
        var db = BuildInMemoryContext(nameof(MarkAttendance_LockedSession_ReturnsFailure));
        var tenantId = Guid.NewGuid();

        var session = new AttendanceSession
        {
            TenantId = tenantId,
            SubjectId = Guid.NewGuid(),
            BatchId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            FacultyUserId = Guid.NewGuid(),
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            PeriodNumber = 1,
            IsLocked = true
        };
        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync();

        var handler = new MarkAttendanceHandler(db);
        var result = await handler.Handle(new MarkAttendanceCommand(
            tenantId,
            session.Id,
            new[] { new AttendanceMark(Guid.NewGuid(), AttendanceStatus.Present) }), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task QrToken_Expired_ReturnsFailure()
    {
        var db = BuildInMemoryContext(nameof(QrToken_Expired_ReturnsFailure));
        var tenantId = Guid.NewGuid();

        var session = new AttendanceSession
        {
            TenantId = tenantId,
            SubjectId = Guid.NewGuid(),
            BatchId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            FacultyUserId = Guid.NewGuid(),
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            PeriodNumber = 1,
            QrToken = "expiredtoken123",
            QrExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };
        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync();

        var handler = new SubmitQrAttendanceHandler(db);
        var result = await handler.Handle(new SubmitQrAttendanceCommand("expiredtoken123", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("QR expired");
    }

    [Fact]
    public async Task QrToken_Valid_MarksPresent()
    {
        var db = BuildInMemoryContext(nameof(QrToken_Valid_MarksPresent));
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        const string token = "validtoken456";

        var session = new AttendanceSession
        {
            TenantId = tenantId,
            SubjectId = Guid.NewGuid(),
            BatchId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            FacultyUserId = Guid.NewGuid(),
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            PeriodNumber = 1,
            QrToken = token,
            QrExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync();

        var handler = new SubmitQrAttendanceHandler(db);
        var result = await handler.Handle(new SubmitQrAttendanceCommand(token, studentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.SessionId == session.Id && r.StudentId == studentId);

        record.Should().NotBeNull();
        record!.Status.Should().Be(AttendanceStatus.Present);
        record.MarkedBy.Should().Be("QR");
    }

    [Fact]
    public async Task Regularization_Approved_UpdatesRecord()
    {
        var db = BuildInMemoryContext(nameof(Regularization_Approved_UpdatesRecord));
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var session = new AttendanceSession
        {
            Id = sessionId,
            TenantId = tenantId,
            SubjectId = Guid.NewGuid(),
            BatchId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            FacultyUserId = Guid.NewGuid(),
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            PeriodNumber = 1,
            IsLocked = true
        };
        db.AttendanceSessions.Add(session);

        var record = new AttendanceRecord
        {
            TenantId = tenantId,
            SessionId = sessionId,
            StudentId = studentId,
            Status = AttendanceStatus.Absent,
            MarkedAt = DateTime.UtcNow,
            MarkedBy = "Faculty"
        };
        db.AttendanceRecords.Add(record);

        var request = new RegularizationRequest
        {
            TenantId = tenantId,
            SessionId = sessionId,
            StudentId = studentId,
            Reason = "Medical leave",
            Status = RegularizationStatus.Pending
        };
        db.RegularizationRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new ReviewRegularizationHandler(db);
        var result = await handler.Handle(new ReviewRegularizationCommand(request.Id, Guid.NewGuid(), true, "Approved"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedRecord = await db.AttendanceRecords.FindAsync(record.Id);
        updatedRecord!.Status.Should().Be(AttendanceStatus.Present);
        updatedRecord.MarkedBy.Should().Be("Regularization");

        var updatedRequest = await db.RegularizationRequests.FindAsync(request.Id);
        updatedRequest!.Status.Should().Be(RegularizationStatus.Approved);
    }
}

// In-memory test DbContext that implements IAttendanceDbContext
internal class TestAttendanceDbContext : DbContext, IAttendanceDbContext
{
    public TestAttendanceDbContext(DbContextOptions<TestAttendanceDbContext> options) : base(options) { }

    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<RegularizationRequest> RegularizationRequests => Set<RegularizationRequest>();
    public DbSet<BiometricLog> BiometricLogs => Set<BiometricLog>();
}
