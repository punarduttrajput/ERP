using ERP.Academics.Application.Commands;
using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Academics;

public class AcademicHierarchyTests
{
    private static IAcademicsDbContext CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<TestAcademicsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mock = new Mock<ICurrentTenant>();
        mock.Setup(x => x.TenantId).Returns(tenantId);
        return new TestAcademicsDbContext(options, mock.Object);
    }

    private static ICurrentTenant MockTenant(Guid tenantId)
    {
        var mock = new Mock<ICurrentTenant>();
        mock.Setup(x => x.TenantId).Returns(tenantId);
        return mock.Object;
    }

    [Fact]
    public async Task CreateDepartment_DuplicateCode_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateContext(tenantId);
        var tenant = MockTenant(tenantId);
        var handler = new CreateDepartmentHandler(db, tenant);

        await handler.Handle(new CreateDepartmentCommand("CS", "Computer Science", null), CancellationToken.None);
        var result = await handler.Handle(new CreateDepartmentCommand("CS", "Computer Science Duplicate", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("CS");
    }

    [Fact]
    public async Task CreateProgram_InvalidDepartmentId_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateContext(tenantId);
        var tenant = MockTenant(tenantId);
        var handler = new CreateProgramHandler(db, tenant);

        var result = await handler.Handle(
            new CreateProgramCommand(Guid.NewGuid(), "BTECH-CSE", "BTech CSE", 4, "Bachelor"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Department");
    }

    [Fact]
    public async Task MapCurriculum_DuplicateEntry_ReturnsFriendlyError()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateContext(tenantId);
        var tenant = MockTenant(tenantId);

        var deptHandler = new CreateDepartmentHandler(db, tenant);
        var deptId = (await deptHandler.Handle(new CreateDepartmentCommand("CSE", "CSE Dept", null), CancellationToken.None)).Value;

        var progHandler = new CreateProgramHandler(db, tenant);
        var progId = (await progHandler.Handle(new CreateProgramCommand(deptId, "BTECH", "BTech", 4, "Bachelor"), CancellationToken.None)).Value;

        var subjHandler = new CreateSubjectHandler(db, tenant);
        var subjId = (await subjHandler.Handle(new CreateSubjectCommand(progId, "CS101", "Programming", 3, 4, "Theory"), CancellationToken.None)).Value;

        var mapHandler = new MapCurriculumHandler(db, tenant);
        await mapHandler.Handle(new MapCurriculumCommand(progId, 1, subjId, false), CancellationToken.None);
        var duplicate = await mapHandler.Handle(new MapCurriculumCommand(progId, 1, subjId, false), CancellationToken.None);

        duplicate.IsSuccess.Should().BeFalse();
        duplicate.Error.Should().Contain("already mapped");
    }

    [Fact]
    public async Task SetCourseOutcomes_ReplacesExistingOnes()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateContext(tenantId);
        var tenant = MockTenant(tenantId);

        var deptHandler = new CreateDepartmentHandler(db, tenant);
        var deptId = (await deptHandler.Handle(new CreateDepartmentCommand("DEPT", "Dept", null), CancellationToken.None)).Value;

        var progHandler = new CreateProgramHandler(db, tenant);
        var progId = (await progHandler.Handle(new CreateProgramCommand(deptId, "PROG", "Program", 4, "Bachelor"), CancellationToken.None)).Value;

        var subjHandler = new CreateSubjectHandler(db, tenant);
        var subjId = (await subjHandler.Handle(new CreateSubjectCommand(progId, "SUB01", "Subject", 3, 4, "Theory"), CancellationToken.None)).Value;

        var coHandler = new SetCourseOutcomesHandler(db, tenant);

        await coHandler.Handle(new SetCourseOutcomesCommand(subjId, new[]
        {
            new OutcomeItem("CO1", "First outcome"),
            new OutcomeItem("CO2", "Second outcome")
        }), CancellationToken.None);

        await coHandler.Handle(new SetCourseOutcomesCommand(subjId, new[]
        {
            new OutcomeItem("CO1", "Replaced outcome")
        }), CancellationToken.None);

        var outcomes = db.CourseOutcomes
            .Where(x => x.SubjectId == subjId && !x.IsDeleted)
            .ToList();

        outcomes.Should().HaveCount(1);
        outcomes[0].Description.Should().Be("Replaced outcome");
    }

    [Fact]
    public async Task CreateAcademicYear_WithIsCurrent_ClearsOtherCurrentYears()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateContext(tenantId);
        var tenant = MockTenant(tenantId);
        var handler = new CreateAcademicYearHandler(db, tenant);

        var firstId = (await handler.Handle(
            new CreateAcademicYearCommand("2025-2026", new DateOnly(2025, 6, 1), new DateOnly(2026, 5, 31), true),
            CancellationToken.None)).Value;

        await handler.Handle(
            new CreateAcademicYearCommand("2026-2027", new DateOnly(2026, 6, 1), new DateOnly(2027, 5, 31), true),
            CancellationToken.None);

        var first = db.AcademicYears.First(x => x.Id == firstId);
        first.IsCurrent.Should().BeFalse();
    }
}
