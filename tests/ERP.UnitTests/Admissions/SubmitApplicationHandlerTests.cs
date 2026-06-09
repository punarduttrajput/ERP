using ERP.Admissions.Application.Commands;
using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ERP.UnitTests.Auth;

namespace ERP.UnitTests.Admissions;

public class SubmitApplicationHandlerTests
{
    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid ProgramId = Guid.NewGuid();
    private static readonly Guid DefId     = Guid.NewGuid();

    [Fact]
    public async Task Submit_NoTenant_ReturnsFailure()
    {
        var tenant = new Mock<ICurrentTenant>();
        tenant.Setup(t => t.TenantId).Returns((Guid?)null);
        var handler = BuildHandler(tenant: tenant);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Tenant context");
    }

    [Fact]
    public async Task Submit_NoSeatsAvailable_ReturnsFailure()
    {
        var handler = BuildHandler(seats: Array.Empty<SeatMatrix>());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("No seat matrix");
    }

    [Fact]
    public async Task Submit_SeatsFull_ReturnsFailure()
    {
        var seat = new SeatMatrix { ProgramId = ProgramId, AcademicYear = 2026, Category = "General", TotalSeats = 10, FilledSeats = 10 };
        var handler = BuildHandler(seats: new[] { seat });

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("No seats available");
    }

    [Fact]
    public async Task Submit_DuplicateApplication_ReturnsFailure()
    {
        var seat = new SeatMatrix { ProgramId = ProgramId, AcademicYear = 2026, Category = "General", TotalSeats = 10, FilledSeats = 0 };
        var existing = new AdmissionApplication
        {
            Id = Guid.NewGuid(), TenantId = TenantId, ProgramId = ProgramId, AcademicYear = 2026,
            ApplicantEmail = "applicant@test.com", State = ApplicationState.Submitted
        };
        var handler = BuildHandler(seats: new[] { seat }, apps: new[] { existing });

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Submit_NoWorkflowDefinition_ReturnsFailure()
    {
        var seat = new SeatMatrix { ProgramId = ProgramId, AcademicYear = 2026, Category = "General", TotalSeats = 10, FilledSeats = 0 };
        var handler = BuildHandler(seats: new[] { seat }, hasDefinition: false);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("workflow definition");
    }

    [Fact]
    public async Task Submit_Valid_ReturnsApplicationId()
    {
        var seat = new SeatMatrix { ProgramId = ProgramId, AcademicYear = 2026, Category = "General", TotalSeats = 10, FilledSeats = 0 };
        var handler = BuildHandler(out var dbMock, seats: new[] { seat });

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        dbMock.Verify(d => d.Applications.AddAsync(
            It.Is<AdmissionApplication>(a => a.State == ApplicationState.Submitted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SubmitApplicationCommand ValidCommand() => new(
        "Alice", "applicant@test.com", "+911234567890",
        ProgramId, "Computer Science", "General", 2026,
        new[] { new DocumentUpload("MarkSheet12", "https://blob/doc1", "marks.pdf") });

    private static SubmitApplicationHandler BuildHandler(
        out Mock<IAdmissionsDbContext> captureDb,
        Mock<ICurrentTenant>? tenant = null,
        SeatMatrix[]? seats = null,
        AdmissionApplication[]? apps = null,
        bool hasDefinition = true)
    {
        captureDb = new Mock<IAdmissionsDbContext>();
        var db = captureDb;

        db.Setup(d => d.SeatMatrices).Returns(
            CreateMockDbSet((seats ?? Array.Empty<SeatMatrix>()).AsQueryable()).Object);
        db.Setup(d => d.Applications).Returns(
            CreateMockDbSet((apps ?? Array.Empty<AdmissionApplication>()).AsQueryable()).Object);
        db.Setup(d => d.WorkflowDefinitions).Returns(
            CreateMockDbSet(hasDefinition
                ? new[] { new WorkflowDefinition { Id = DefId, TenantId = TenantId, Name = "Default", Version = 1, IsActive = true } }.AsQueryable()
                : Array.Empty<WorkflowDefinition>().AsQueryable()).Object);
        db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        if (tenant is null)
        {
            tenant = new Mock<ICurrentTenant>();
            tenant.Setup(t => t.TenantId).Returns(TenantId);
        }
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.UserId).Returns(Guid.NewGuid());

        return new SubmitApplicationHandler(
            db.Object, tenant.Object, user.Object,
            new Mock<ILogger<SubmitApplicationHandler>>().Object);
    }

    private static SubmitApplicationHandler BuildHandler(
        Mock<ICurrentTenant>? tenant = null,
        SeatMatrix[]? seats = null,
        AdmissionApplication[]? apps = null,
        bool hasDefinition = true) =>
        BuildHandler(out _, tenant, seats, apps, hasDefinition);

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mock = new Mock<DbSet<T>>();
        mock.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mock;
    }
}
