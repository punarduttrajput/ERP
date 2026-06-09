using ERP.Accreditation.Application.Commands;
using ERP.Accreditation.Application.Queries;
using ERP.Accreditation.Domain;
using ERP.Accreditation.Infrastructure;
using ERP.Shared.Application.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Accreditation;

public class EvidenceRegistryTests
{
    private static IAccreditationDbContext CreateInMemoryContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<TestAccreditationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestAccreditationDbContext(options, tenantId);
    }

    [Fact]
    public async Task TagEvidence_ValidCriterion_InsertsTag()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateInMemoryContext(tenantId);
        var handler = new TagEvidenceHandler(db);

        var command = new TagEvidenceCommand(
            TenantId: tenantId,
            ModuleName: "SIS",
            RecordId: "student-001",
            RecordLabel: "Alice Williams — B.Tech CSE 2026",
            NaacCriterion: "2.1",
            NaacIndicator: "2.1.1",
            Notes: null,
            TaggedBy: Guid.NewGuid()
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var tag = await db.EvidenceTags.FirstOrDefaultAsync(t => t.Id == result.Value);
        tag.Should().NotBeNull();
        tag!.NaacCriterion.Should().Be("2.1");
        tag.NaacIndicator.Should().Be("2.1.1");
    }

    [Fact]
    public async Task TagEvidence_InvalidCriterionFormat_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateInMemoryContext(tenantId);
        var handler = new TagEvidenceHandler(db);

        var command = new TagEvidenceCommand(
            TenantId: tenantId,
            ModuleName: "SIS",
            RecordId: "student-001",
            RecordLabel: "Test",
            NaacCriterion: "99.99",
            NaacIndicator: "99.99.1",
            Notes: null,
            TaggedBy: Guid.NewGuid()
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("99.99");
    }

    [Fact]
    public async Task RefreshSummary_CallsAllProviders()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateInMemoryContext(tenantId);

        var provider1 = new Mock<IEvidenceProvider>();
        provider1.Setup(p => p.ModuleName).Returns("SIS");
        provider1.Setup(p => p.GetEvidenceAsync(tenantId, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceItem>
            {
                new("SIS", "StudentEnrollment", "s1", "Alice", null, "B.Tech", DateTime.UtcNow),
                new("SIS", "StudentEnrollment", "s2", "Bob", null, "M.Tech", DateTime.UtcNow)
            });

        var provider2 = new Mock<IEvidenceProvider>();
        provider2.Setup(p => p.ModuleName).Returns("Exams");
        provider2.Setup(p => p.GetEvidenceAsync(tenantId, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceItem>
            {
                new("Exams", "ExamResult", "r1", "Math — A", 88.5m, null, DateTime.UtcNow),
                new("Exams", "ExamResult", "r2", "Physics — B", 72.0m, null, DateTime.UtcNow)
            });

        var handler = new RefreshEvidenceSummaryHandler(db, new[] { provider1.Object, provider2.Object });

        var result = await handler.Handle(
            new RefreshEvidenceSummaryCommand(tenantId, 2026, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        provider1.Verify(p => p.GetEvidenceAsync(tenantId, 2026, It.IsAny<CancellationToken>()), Times.Once);
        provider2.Verify(p => p.GetEvidenceAsync(tenantId, 2026, It.IsAny<CancellationToken>()), Times.Once);

        var summaries = await db.EvidenceSummaries.ToListAsync();
        summaries.Should().NotBeEmpty();

        // SIS: count only (no numeric); Exams: count + sum + avg
        summaries.Count.Should().Be(4);
    }

    [Fact]
    public async Task GetCoverage_ReturnsCorrectPercentage()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateInMemoryContext(tenantId);

        var taggedBy = Guid.NewGuid();
        db.EvidenceTags.Add(new EvidenceTag { TenantId = tenantId, ModuleName = "SIS", RecordId = "r1", RecordLabel = "L1", NaacCriterion = "1.1", NaacIndicator = "1.1.1", TaggedBy = taggedBy });
        db.EvidenceTags.Add(new EvidenceTag { TenantId = tenantId, ModuleName = "SIS", RecordId = "r2", RecordLabel = "L2", NaacCriterion = "1.1", NaacIndicator = "1.1.2", TaggedBy = taggedBy });
        db.EvidenceTags.Add(new EvidenceTag { TenantId = tenantId, ModuleName = "Exams", RecordId = "r3", RecordLabel = "L3", NaacCriterion = "2.1", NaacIndicator = "2.1.1", TaggedBy = taggedBy });
        db.EvidenceTags.Add(new EvidenceTag { TenantId = tenantId, ModuleName = "Exams", RecordId = "r4", RecordLabel = "L4", NaacCriterion = "2.1", NaacIndicator = "2.1.2", TaggedBy = taggedBy });
        await db.SaveChangesAsync();

        var handler = new GetEvidenceCoverageHandler(db);
        var result = await handler.Handle(new GetEvidenceCoverageQuery(tenantId), CancellationToken.None);

        result.Should().HaveCount(7);

        var c1 = result.First(r => r.Criterion == "1");
        c1.TotalIndicators.Should().Be(3);
        c1.TaggedIndicators.Should().Be(2);
        c1.CoveragePercent.Should().Be(Math.Round(2m / 3m * 100, 2));

        var c2 = result.First(r => r.Criterion == "2");
        c2.TotalIndicators.Should().Be(4);
        c2.TaggedIndicators.Should().Be(2);
        c2.CoveragePercent.Should().Be(50m);
    }

    [Fact]
    public async Task GetEvidenceTags_FiltersByCriterion()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateInMemoryContext(tenantId);

        var taggedBy = Guid.NewGuid();
        db.EvidenceTags.Add(new EvidenceTag { TenantId = tenantId, ModuleName = "SIS", RecordId = "r1", RecordLabel = "L1", NaacCriterion = "1.1", NaacIndicator = "1.1.1", TaggedBy = taggedBy });
        db.EvidenceTags.Add(new EvidenceTag { TenantId = tenantId, ModuleName = "SIS", RecordId = "r2", RecordLabel = "L2", NaacCriterion = "1.1", NaacIndicator = "1.1.2", TaggedBy = taggedBy });
        db.EvidenceTags.Add(new EvidenceTag { TenantId = tenantId, ModuleName = "Exams", RecordId = "r3", RecordLabel = "L3", NaacCriterion = "2.1", NaacIndicator = "2.1.1", TaggedBy = taggedBy });
        await db.SaveChangesAsync();

        var handler = new GetEvidenceTagsHandler(db);
        var result = await handler.Handle(
            new GetEvidenceTagsQuery(tenantId, "1.1", null, 1, 20),
            CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(t => t.NaacCriterion.Should().Be("1.1"));
    }
}

// Minimal in-memory DbContext for tests — avoids pulling in the full AppDbContext.
internal class TestAccreditationDbContext : DbContext, IAccreditationDbContext
{
    private readonly Guid _tenantId;

    public TestAccreditationDbContext(DbContextOptions<TestAccreditationDbContext> options, Guid tenantId)
        : base(options)
    {
        _tenantId = tenantId;
    }

    public DbSet<EvidenceTag> EvidenceTags => Set<EvidenceTag>();
    public DbSet<EvidenceSummary> EvidenceSummaries => Set<EvidenceSummary>();
}
