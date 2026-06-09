using ERP.NAAC.Application.Commands;
using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.NAAC;

public class NaacTests
{
    private static INaacDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NaacTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NaacTestDbContext(options);
    }

    [Fact]
    public async Task CreateSsr_CreatesAllCriterionSections()
    {
        var db = CreateDb();
        var handler = new CreateSsrHandler(db);
        var tenantId = Guid.NewGuid();
        var expectedSectionCount = NaacCriteria.All.Sum(c => c.Indicators.Length);

        var result = await handler.Handle(
            new CreateSsrCommand(tenantId, 2025, "SSR 2025 — Test University"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var sections = await db.SsrSections.ToListAsync();
        sections.Should().HaveCount(expectedSectionCount);

        foreach (var criterion in NaacCriteria.All)
        {
            foreach (var indicator in criterion.Indicators)
            {
                sections.Should().Contain(s =>
                    s.CriterionNumber == criterion.Number && s.IndicatorNumber == indicator);
            }
        }
    }

    [Fact]
    public async Task GenerateSsrPdf_ReturnsNonEmptyBytes()
    {
        var db = CreateDb();
        var createHandler = new CreateSsrHandler(db);
        var tenantId = Guid.NewGuid();

        var createResult = await createHandler.Handle(
            new CreateSsrCommand(tenantId, 2025, "SSR 2025 — Test University"),
            CancellationToken.None);

        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header
        var mockPdf = new Mock<IPdfService>();
        mockPdf.Setup(p => p.GeneratePdfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(pdfBytes);

        var pdfHandler = new GenerateSsrPdfHandler(db, mockPdf.Object);
        var result = await pdfHandler.Handle(
            new GenerateSsrPdfCommand(createResult.Value!),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RespondToDvvQuery_SetsStatusResponded()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var ssrId = Guid.NewGuid();

        var createHandler = new CreateDvvQueryHandler(db);
        var createResult = await createHandler.Handle(
            new CreateDvvQueryCommand(tenantId, ssrId, "DVV-001", "2", "2.6",
                "Please clarify pass rate data.", DateTime.UtcNow),
            CancellationToken.None);

        var respondHandler = new RespondToDvvQueryHandler(db);
        var respondResult = await respondHandler.Handle(
            new RespondToDvvQueryCommand(createResult.Value!, "Pass rate is 87%.", Guid.NewGuid()),
            CancellationToken.None);

        respondResult.IsSuccess.Should().BeTrue();

        var query = await db.DvvQueries.FirstAsync(q => q.Id == createResult.Value);
        query.Status.Should().Be(DvvStatus.Responded);
        query.RespondedAt.Should().NotBeNull();
        query.Response.Should().Be("Pass rate is 87%.");
    }

    [Fact]
    public async Task SubmitAqarSection_SetsUnderReview()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var createHandler = new CreateAqarHandler(db);
        var createResult = await createHandler.Handle(
            new CreateAqarCommand(tenantId, 2025, "AQAR 2025"),
            CancellationToken.None);

        var aqar = await db.AqarReports.Include(a => a.Sections)
            .FirstAsync(a => a.Id == createResult.Value);
        var section = aqar.Sections.First();

        var assignHandler = new AssignAqarSectionHandler(db);
        await assignHandler.Handle(
            new AssignAqarSectionCommand(aqar.Id, section.Id, Guid.NewGuid()),
            CancellationToken.None);

        var submitHandler = new SubmitAqarSectionHandler(db);
        var result = await submitHandler.Handle(
            new SubmitAqarSectionCommand(aqar.Id, section.Id, "Content for criterion 1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.AqarSections.FirstAsync(s => s.Id == section.Id);
        updated.Status.Should().Be(AqarStatus.UnderReview);
        updated.Content.Should().Be("Content for criterion 1");
    }

    [Fact]
    public async Task ReviewAqarSection_Reject_SetsBackToInProgress()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var createResult = await new CreateAqarHandler(db).Handle(
            new CreateAqarCommand(tenantId, 2025, "AQAR 2025"), CancellationToken.None);

        var aqar = await db.AqarReports.Include(a => a.Sections)
            .FirstAsync(a => a.Id == createResult.Value);
        var section = aqar.Sections.First();

        await new AssignAqarSectionHandler(db).Handle(
            new AssignAqarSectionCommand(aqar.Id, section.Id, Guid.NewGuid()), CancellationToken.None);

        await new SubmitAqarSectionHandler(db).Handle(
            new SubmitAqarSectionCommand(aqar.Id, section.Id, "Draft content"), CancellationToken.None);

        var reviewHandler = new ReviewAqarSectionHandler(db);
        var result = await reviewHandler.Handle(
            new ReviewAqarSectionCommand(aqar.Id, section.Id, false, Guid.NewGuid(), "Needs more detail"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.AqarSections.FirstAsync(s => s.Id == section.Id);
        updated.Status.Should().Be(AqarStatus.InProgress);
        updated.ReviewComment.Should().Be("Needs more detail");
    }

    [Fact]
    public async Task FinaliseAqar_AllSectionsApproved_SetsApproved()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var createResult = await new CreateAqarHandler(db).Handle(
            new CreateAqarCommand(tenantId, 2025, "AQAR 2025"), CancellationToken.None);

        var aqar = await db.AqarReports.Include(a => a.Sections)
            .FirstAsync(a => a.Id == createResult.Value);

        foreach (var section in aqar.Sections)
        {
            await new AssignAqarSectionHandler(db).Handle(
                new AssignAqarSectionCommand(aqar.Id, section.Id, Guid.NewGuid()), CancellationToken.None);
            await new SubmitAqarSectionHandler(db).Handle(
                new SubmitAqarSectionCommand(aqar.Id, section.Id, "Approved content"), CancellationToken.None);
            await new ReviewAqarSectionHandler(db).Handle(
                new ReviewAqarSectionCommand(aqar.Id, section.Id, true, Guid.NewGuid()), CancellationToken.None);
        }

        var result = await new FinaliseAqarHandler(db).Handle(
            new FinaliseAqarCommand(aqar.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.AqarReports.FirstAsync(a => a.Id == aqar.Id);
        updated.Status.Should().Be(AqarStatus.Approved);
    }

    [Fact]
    public async Task FinaliseAqar_SomeSectionsPending_ReturnsFailure()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var createResult = await new CreateAqarHandler(db).Handle(
            new CreateAqarCommand(tenantId, 2025, "AQAR 2025"), CancellationToken.None);

        // Do not approve any sections — all remain in Draft
        var result = await new FinaliseAqarHandler(db).Handle(
            new FinaliseAqarCommand(createResult.Value!), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Approved");
    }
}

// Minimal in-memory DbContext implementing INaacDbContext for unit tests
internal class NaacTestDbContext : DbContext, INaacDbContext
{
    public NaacTestDbContext(DbContextOptions<NaacTestDbContext> options) : base(options) { }

    public DbSet<ERP.NAAC.Domain.SsrReport> SsrReports => Set<ERP.NAAC.Domain.SsrReport>();
    public DbSet<ERP.NAAC.Domain.SsrSection> SsrSections => Set<ERP.NAAC.Domain.SsrSection>();
    public DbSet<ERP.NAAC.Domain.DvvQuery> DvvQueries => Set<ERP.NAAC.Domain.DvvQuery>();
    public DbSet<ERP.NAAC.Domain.AqarReport> AqarReports => Set<ERP.NAAC.Domain.AqarReport>();
    public DbSet<ERP.NAAC.Domain.AqarSection> AqarSections => Set<ERP.NAAC.Domain.AqarSection>();
}
