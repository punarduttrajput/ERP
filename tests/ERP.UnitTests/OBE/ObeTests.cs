using ERP.OBE.Application.Commands;
using ERP.OBE.Application.Queries;
using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.OBE;

public class ObeTests
{
    private static IObeDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<TestObeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestObeDbContext(options);
    }

    [Fact]
    public async Task SetCoPoMapping_ValidLevels_Upserts()
    {
        var db = CreateInMemoryDb();
        var handler = new SetCoPoMappingHandler(db);
        var tenantId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var command = new SetCoPoMappingCommand(
            tenantId, subjectId, programId,
            new List<CoPoMappingItem> { new("CO1", "PO1", 3) });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = db.CoPoMappings.Single();
        stored.CourseOutcomeCode.Should().Be("CO1");
        stored.ProgramOutcomeCode.Should().Be("PO1");
        stored.CorrelationLevel.Should().Be(3);
    }

    [Fact]
    public async Task SetCoPoMapping_InvalidLevel_ReturnsFailure()
    {
        var db = CreateInMemoryDb();
        var handler = new SetCoPoMappingHandler(db);

        var command = new SetCoPoMappingCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new List<CoPoMappingItem> { new("CO1", "PO1", 5) });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("0-3");
    }

    [Theory]
    [InlineData(30, AttainmentLevel.NotMet)]
    [InlineData(50, AttainmentLevel.PartiallyMet)]
    [InlineData(65, AttainmentLevel.Met)]
    [InlineData(80, AttainmentLevel.Exceeded)]
    public void ComputeAttainmentLevel_BelowThreshold_NotMet(decimal percent, AttainmentLevel expected)
    {
        var level = ComputeDirectAttainmentHandler.ComputeLevel(percent);
        level.Should().Be(expected);
    }

    [Fact]
    public void ComputeAttainmentLevel_AboveTarget_Exceeded()
    {
        var level = ComputeDirectAttainmentHandler.ComputeLevel(80m);
        level.Should().Be(AttainmentLevel.Exceeded);
    }

    [Fact]
    public async Task GenerateActionPlan_GapExists_CreatesDescriptionWithRemedial()
    {
        var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        db.AttainmentGaps.Add(new AttainmentGap
        {
            TenantId = tenantId,
            SubjectId = subjectId,
            CourseOutcomeCode = "CO1",
            SemesterId = semesterId,
            AcademicYear = 2026,
            DirectAttainmentPercent = 30m,
            CombinedAttainmentPercent = 30m,
            TargetPercent = 60m,
            GapPercent = 30m,
            Level = AttainmentLevel.NotMet
        });
        await db.SaveChangesAsync();

        var handler = new GenerateActionPlanHandler(db);
        var result = await handler.Handle(
            new GenerateActionPlanCommand(tenantId, subjectId, semesterId, 2026),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        var plan = db.ActionPlans.Single();
        plan.Description.Should().Contain("remedial");
    }

    [Fact]
    public void ComputePoAttainment_WeightedAverage_CorrectResult()
    {
        var subjectId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var mappings = new List<CoPoMapping>
        {
            new() { TenantId = tenantId, SubjectId = subjectId, ProgramId = programId, CourseOutcomeCode = "CO1", ProgramOutcomeCode = "PO1", CorrelationLevel = 3 },
            new() { TenantId = tenantId, SubjectId = subjectId, ProgramId = programId, CourseOutcomeCode = "CO2", ProgramOutcomeCode = "PO1", CorrelationLevel = 1 }
        };

        var semesterId = Guid.NewGuid();
        var attainments = new List<DirectAttainment>
        {
            new() { TenantId = tenantId, SubjectId = subjectId, CourseOutcomeCode = "CO1", SemesterId = semesterId, AttainmentPercent = 80m },
            new() { TenantId = tenantId, SubjectId = subjectId, CourseOutcomeCode = "CO2", SemesterId = semesterId, AttainmentPercent = 60m }
        };

        var result = GetNbaReportHandler.ComputePoAttainments(mappings, attainments);

        // PO1 = (80*3 + 60*1) / (3+1) = (240 + 60) / 4 = 300 / 4 = 75
        result.Should().ContainKey("PO1");
        result["PO1"].Should().Be(75m);
    }
}

// In-memory test implementation of IObeDbContext using EF Core InMemory provider
internal class TestObeDbContext : DbContext, IObeDbContext
{
    public TestObeDbContext(DbContextOptions<TestObeDbContext> options) : base(options) { }

    public DbSet<CoPoMapping> CoPoMappings => Set<CoPoMapping>();
    public DbSet<CoPsoMapping> CoPsoMappings => Set<CoPsoMapping>();
    public DbSet<DirectAttainment> DirectAttainments => Set<DirectAttainment>();
    public DbSet<IndirectAttainmentSurvey> IndirectSurveys => Set<IndirectAttainmentSurvey>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<AttainmentGap> AttainmentGaps => Set<AttainmentGap>();
    public DbSet<ActionPlan> ActionPlans => Set<ActionPlan>();
}
