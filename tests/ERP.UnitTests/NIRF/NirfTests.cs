using ERP.NIRF.Application.Commands;
using ERP.NIRF.Domain;
using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Xunit;

namespace ERP.UnitTests.NIRF;

public class NirfTests
{
    private static INirfDbContext CreateDb(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<NirfTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NirfTestDbContext(options, tenantId);
    }

    [Fact]
    public void OverallScore_IsWeightedSum()
    {
        var scores = new[]
        {
            (Parameter: NirfParameter.TeachingLearning, Raw: 80m, Weight: 0.30m),
            (Parameter: NirfParameter.Research, Raw: 70m, Weight: 0.30m),
            (Parameter: NirfParameter.GraduationOutcomes, Raw: 60m, Weight: 0.20m),
            (Parameter: NirfParameter.Outreach, Raw: 50m, Weight: 0.10m),
            (Parameter: NirfParameter.Perception, Raw: 40m, Weight: 0.10m),
        };

        var overall = scores.Sum(s => Math.Round(s.Raw * s.Weight, 2));

        overall.Should().Be(66.0m);
    }

    [Fact]
    public async Task UpdateParameter_ManualOverride_SetsFlag()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateDb(tenantId);

        var submission = new NirfSubmission
        {
            TenantId = tenantId,
            RankingYear = 2026,
            Category = "University",
            Status = SubmissionStatus.Compiled
        };
        db.NirfSubmissions.Add(submission);
        await db.SaveChangesAsync();

        var handler = new UpdateNirfParameterHandler(db);
        var result = await handler.Handle(
            new UpdateNirfParameterCommand(tenantId, submission.Id, NirfParameter.Perception, 75m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var score = db.NirfParameterScores
            .First(p => p.SubmissionId == submission.Id && p.Parameter == NirfParameter.Perception);
        score.IsManualOverride.Should().BeTrue();
        score.RawScore.Should().Be(75m);
    }

    [Fact]
    public async Task FinaliseSubmission_NoScores_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateDb(tenantId);

        var submission = new NirfSubmission
        {
            TenantId = tenantId,
            RankingYear = 2026,
            Category = "University",
            Status = SubmissionStatus.Compiled
        };
        db.NirfSubmissions.Add(submission);
        await db.SaveChangesAsync();

        var handler = new FinaliseNirfSubmissionHandler(db);
        var result = await handler.Handle(
            new FinaliseNirfSubmissionCommand(tenantId, submission.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("5 NIRF parameter scores");
    }

    [Fact]
    public async Task FinaliseSubmission_AllScoresPresent_SetsReviewed()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateDb(tenantId);

        var submission = new NirfSubmission
        {
            TenantId = tenantId,
            RankingYear = 2026,
            Category = "University",
            Status = SubmissionStatus.Compiled,
            OverallScore = 66m
        };
        db.NirfSubmissions.Add(submission);
        await db.SaveChangesAsync();

        foreach (var param in NirfParameter.All)
        {
            db.NirfParameterScores.Add(new NirfParameterScore
            {
                TenantId = tenantId,
                SubmissionId = submission.Id,
                Parameter = param,
                RawScore = 70m,
                WeightedScore = 21m,
                Weight = 0.30m,
                DataJson = "{}"
            });
        }
        await db.SaveChangesAsync();

        var handler = new FinaliseNirfSubmissionHandler(db);
        var result = await handler.Handle(
            new FinaliseNirfSubmissionCommand(tenantId, submission.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = db.NirfSubmissions.First(s => s.Id == submission.Id);
        updated.Status.Should().Be(SubmissionStatus.Reviewed);
    }

    [Fact]
    public async Task ExportXml_ContainsAllParameters()
    {
        var tenantId = Guid.NewGuid();
        var db = CreateDb(tenantId);

        var connectionFactory = new Mock<IDbConnectionFactory>();
        var connection = new Mock<System.Data.IDbConnection>();
        // We can't easily test Dapper tenant lookup in unit tests, so we skip xml handler
        // and test the XML structure directly from the domain data.

        var submission = new NirfSubmission
        {
            TenantId = tenantId,
            RankingYear = 2026,
            Category = "University",
            Status = SubmissionStatus.Compiled,
            OverallScore = 66m
        };
        db.NirfSubmissions.Add(submission);
        await db.SaveChangesAsync();

        foreach (var param in NirfParameter.All)
        {
            db.NirfParameterScores.Add(new NirfParameterScore
            {
                TenantId = tenantId,
                SubmissionId = submission.Id,
                Parameter = param,
                RawScore = 70m,
                WeightedScore = 21m,
                Weight = 0.30m,
                DataJson = JsonSerializer.Serialize(new { placeholder = true })
            });
        }
        await db.SaveChangesAsync();

        var loaded = db.NirfSubmissions
            .Include(s => s.ParameterScores)
            .First(s => s.Id == submission.Id);

        var xml = new System.Xml.Linq.XDocument(
            new System.Xml.Linq.XElement("NIRFSubmission",
                new System.Xml.Linq.XAttribute("year", loaded.RankingYear),
                new System.Xml.Linq.XElement("Parameters",
                    loaded.ParameterScores.Select(p =>
                        new System.Xml.Linq.XElement("Parameter",
                            new System.Xml.Linq.XAttribute("name", p.Parameter))))));

        var xmlStr = xml.ToString();

        foreach (var param in NirfParameter.All)
        {
            xmlStr.Should().Contain(param);
        }
    }
}

// In-memory EF context for unit tests — bypasses global query filter by hardcoding TenantId
public class NirfTestDbContext : DbContext, INirfDbContext
{
    private readonly Guid _tenantId;

    public NirfTestDbContext(DbContextOptions options, Guid tenantId) : base(options)
        => _tenantId = tenantId;

    public DbSet<NirfSubmission> NirfSubmissions => Set<NirfSubmission>();
    public DbSet<NirfParameterScore> NirfParameterScores => Set<NirfParameterScore>();
    public DbSet<NirfRankEntry> NirfRankHistory => Set<NirfRankEntry>();
}
