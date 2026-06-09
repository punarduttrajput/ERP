using ERP.Academics.Application.Commands;
using ERP.Academics.Application.Queries;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Academics;

public class CurriculumQueryTests
{
    private static (IAcademicsDbContext db, ICurrentTenant tenant) CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<TestAcademicsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var mock = new Mock<ICurrentTenant>();
        mock.Setup(x => x.TenantId).Returns(tenantId);
        var tenant = mock.Object;
        return (new TestAcademicsDbContext(options, mock.Object), tenant);
    }

    [Fact]
    public async Task GetCurriculum_ReturnsOnlySubjectsForSpecifiedProgramAndSemester()
    {
        var tenantId = Guid.NewGuid();
        var (db, tenant) = CreateContext(tenantId);

        var deptId = (await new CreateDepartmentHandler(db, tenant)
            .Handle(new CreateDepartmentCommand("EE", "Electrical", null), default)).Value;

        var prog1Id = (await new CreateProgramHandler(db, tenant)
            .Handle(new CreateProgramCommand(deptId, "BTECH-EE", "BTech EE", 4, "Bachelor"), default)).Value;

        var prog2Id = (await new CreateProgramHandler(db, tenant)
            .Handle(new CreateProgramCommand(deptId, "MTECH-EE", "MTech EE", 2, "Master"), default)).Value;

        var subj1Id = (await new CreateSubjectHandler(db, tenant)
            .Handle(new CreateSubjectCommand(prog1Id, "EE101", "Circuits", 3, 4, "Theory"), default)).Value;

        var subj2Id = (await new CreateSubjectHandler(db, tenant)
            .Handle(new CreateSubjectCommand(prog1Id, "EE102", "Signals", 3, 4, "Theory"), default)).Value;

        var subj3Id = (await new CreateSubjectHandler(db, tenant)
            .Handle(new CreateSubjectCommand(prog2Id, "EE201", "Advanced Circuits", 4, 5, "Theory"), default)).Value;

        var mapHandler = new MapCurriculumHandler(db, tenant);
        await mapHandler.Handle(new MapCurriculumCommand(prog1Id, 1, subj1Id, false), default);
        await mapHandler.Handle(new MapCurriculumCommand(prog1Id, 2, subj2Id, false), default);
        await mapHandler.Handle(new MapCurriculumCommand(prog2Id, 1, subj3Id, false), default);

        var queryHandler = new GetCurriculumHandler(db);

        var result = await queryHandler.Handle(new GetCurriculumQuery(prog1Id, 1), default);

        result.Should().HaveCount(1);
        result[0].SubjectId.Should().Be(subj1Id);
    }

    [Fact]
    public async Task GetCurriculum_NoEntries_ReturnsEmptyList()
    {
        var tenantId = Guid.NewGuid();
        var (db, _) = CreateContext(tenantId);

        var queryHandler = new GetCurriculumHandler(db);
        var result = await queryHandler.Handle(new GetCurriculumQuery(Guid.NewGuid(), 1), default);

        result.Should().BeEmpty();
    }
}
