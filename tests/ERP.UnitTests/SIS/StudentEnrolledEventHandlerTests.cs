using ERP.Admissions.Application.Events;
using ERP.SIS.Application.Events;
using ERP.SIS.Domain;
using ERP.SIS.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERP.UnitTests.SIS;

public class StudentEnrolledEventHandlerTests
{
    private static ISisDbContext BuildDb(IEnumerable<Student>? existing = null)
    {
        var options = new DbContextOptionsBuilder<SisTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new SisTestDbContext(options);
        if (existing is not null)
            ctx.Students.AddRange(existing);
        ctx.SaveChanges();
        return ctx;
    }

    private static StudentEnrolledEventHandler BuildHandler(ISisDbContext db)
        => new(db, Mock.Of<ILogger<StudentEnrolledEventHandler>>());

    private static StudentEnrolledEvent ValidEvent(Guid tenantId) => new(
        ApplicationId: Guid.NewGuid(),
        TenantId: tenantId,
        ApplicantName: "Ravi Kumar",
        ApplicantEmail: "ravi@example.com",
        ApplicantMobile: "9876543210",
        ProgramId: Guid.NewGuid(),
        ProgramName: "B.Tech CSE",
        AcademicYear: 2026
    );

    [Fact]
    public async Task Handle_ValidEvent_CreatesStudentWithCorrectNumberFormat()
    {
        var tenantId = Guid.NewGuid();
        var db = BuildDb();
        var handler = BuildHandler(db);
        var evt = ValidEvent(tenantId);

        await handler.Handle(evt, CancellationToken.None);

        var student = await ((SisTestDbContext)db).Students.IgnoreQueryFilters().FirstOrDefaultAsync();
        student.Should().NotBeNull();
        student!.StudentNumber.Should().MatchRegex(@"^\d{4}-[A-Z0-9]{4}-\d{5}$");
        student.StudentNumber.Should().StartWith($"{evt.AcademicYear}-");
        student.FirstName.Should().Be("Ravi");
        student.LastName.Should().Be("Kumar");
        student.Email.Should().Be(evt.ApplicantEmail);
        student.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Handle_EmptyTenantId_SkipsCreation()
    {
        var db = BuildDb();
        var handler = BuildHandler(db);
        var evt = ValidEvent(Guid.Empty);

        await handler.Handle(evt, CancellationToken.None);

        var count = await ((SisTestDbContext)db).Students.IgnoreQueryFilters().CountAsync();
        count.Should().Be(0);
    }
}

// Minimal in-memory DbContext implementing ISisDbContext for tests.
internal class SisTestDbContext : DbContext, ISisDbContext
{
    public SisTestDbContext(DbContextOptions<SisTestDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<StudentFamily> StudentFamilyDetails => Set<StudentFamily>();
}
