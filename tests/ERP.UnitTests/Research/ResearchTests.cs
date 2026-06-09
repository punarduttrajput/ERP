using ERP.Research.Application.Commands;
using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.UnitTests.Research;

public class ResearchTests
{
    private static IResearchDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ResearchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ResearchTestDbContext(options);
    }

    [Fact]
    public async Task CreatePublication_FutureYear_ReturnsFailure()
    {
        var db = CreateDb();
        var handler = new CreatePublicationHandler(db);
        var futureYear = DateTime.UtcNow.Year + 2;

        var result = await handler.Handle(new CreatePublicationCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "Some Title",
            PublicationType.Journal, "Nature", null, null, null,
            futureYear, null, null, PublicationIndex.Scopus, false, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("1900", result.Error);
    }

    [Fact]
    public async Task CreatePublication_NegativeImpactFactor_ReturnsFailure()
    {
        var db = CreateDb();
        var handler = new CreatePublicationHandler(db);

        var result = await handler.Handle(new CreatePublicationCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "Some Title",
            PublicationType.Journal, "Nature", null, null, null,
            2024, null, -1m, PublicationIndex.Scopus, false, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("negative", result.Error);
    }

    [Fact]
    public async Task UpdatePatentStatus_FiledToGranted_RequiresGrantNumber()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var patent = new Patent
        {
            TenantId = tenantId,
            Title = "Test Patent",
            Inventors = "John Doe",
            PatentOffice = "IPO",
            Status = PatentStatus.UnderExamination
        };
        await db.Patents.AddAsync(patent);
        await db.SaveChangesAsync();

        var handler = new UpdatePatentStatusHandler(db);

        var result = await handler.Handle(new UpdatePatentStatusCommand(
            tenantId, patent.Id, PatentStatus.Granted, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Grant number", result.Error);
    }

    [Fact]
    public async Task RecordDisbursement_OverSanctionedAmount_ReturnsFailure()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var grant = new Grant
        {
            TenantId = tenantId,
            Title = "Test Grant",
            FundingAgency = "DST",
            SanctionedAmount = 100_000m,
            DisbursedAmount = 90_000m,
            UtilizedAmount = 0m,
            Status = GrantStatus.Active,
            PrincipalInvestigatorId = Guid.NewGuid()
        };
        await db.Grants.AddAsync(grant);
        await db.SaveChangesAsync();

        var handler = new RecordDisbursementHandler(db);

        var result = await handler.Handle(new RecordDisbursementCommand(
            tenantId, grant.Id, 20_000m,
            DateOnly.FromDateTime(DateTime.UtcNow), null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("exceed", result.Error);
    }

    [Fact]
    public async Task RecordDisbursement_Valid_UpdatesGrantTotal()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var grant = new Grant
        {
            TenantId = tenantId,
            Title = "Test Grant",
            FundingAgency = "DST",
            SanctionedAmount = 100_000m,
            DisbursedAmount = 0m,
            UtilizedAmount = 0m,
            Status = GrantStatus.Approved,
            PrincipalInvestigatorId = Guid.NewGuid()
        };
        await db.Grants.AddAsync(grant);
        await db.SaveChangesAsync();

        var handler = new RecordDisbursementHandler(db);

        var result = await handler.Handle(new RecordDisbursementCommand(
            tenantId, grant.Id, 40_000m,
            DateOnly.FromDateTime(DateTime.UtcNow), "REF001", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.Grants.IgnoreQueryFilters().FirstAsync(x => x.Id == grant.Id);
        Assert.Equal(40_000m, updated.DisbursedAmount);
        Assert.Equal(GrantStatus.Active, updated.Status);
    }

    [Fact]
    public async Task CloseGrant_UtilizedExceedsDisbursed_ReturnsFailure()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var grant = new Grant
        {
            TenantId = tenantId,
            Title = "Test Grant",
            FundingAgency = "DST",
            SanctionedAmount = 100_000m,
            DisbursedAmount = 60_000m,
            UtilizedAmount = 0m,
            Status = GrantStatus.Active,
            PrincipalInvestigatorId = Guid.NewGuid()
        };
        await db.Grants.AddAsync(grant);
        await db.SaveChangesAsync();

        var handler = new CloseGrantHandler(db);

        var result = await handler.Handle(new CloseGrantCommand(
            tenantId, grant.Id, 80_000m, "UC/2025/001"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("disbursed", result.Error);
    }

    [Fact]
    public async Task ProjectStatusTransition_InvalidTransition_ReturnsFailure()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var project = new ResearchProject
        {
            TenantId = tenantId,
            Title = "ML Research",
            PrincipalInvestigatorId = Guid.NewGuid(),
            PrincipalInvestigatorName = "Prof. Rao",
            FundingAgency = "SERB",
            SanctionedAmount = 500_000m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ProjectStatus.Completed
        };
        await db.ResearchProjects.AddAsync(project);
        await db.SaveChangesAsync();

        var handler = new UpdateProjectStatusHandler(db);

        var result = await handler.Handle(new UpdateProjectStatusCommand(
            tenantId, project.Id, ProjectStatus.Active, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not allowed", result.Error);
    }
}

// Minimal in-memory DbContext for unit tests — avoids the tenant resolution infrastructure
internal class ResearchTestDbContext : DbContext, IResearchDbContext
{
    public ResearchTestDbContext(DbContextOptions<ResearchTestDbContext> options) : base(options) { }

    public DbSet<ResearchProject> ResearchProjects => Set<ResearchProject>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<Patent> Patents => Set<Patent>();
    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<GrantDisbursement> GrantDisbursements => Set<GrantDisbursement>();
}
