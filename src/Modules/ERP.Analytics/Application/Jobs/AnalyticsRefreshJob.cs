using Dapper;
using ERP.Analytics.Application.Commands;
using ERP.Analytics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ERP.Analytics.Application.Jobs;

public class AnalyticsRefreshJob
{
    private readonly IMediator _mediator;
    private readonly IAnalyticsDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AnalyticsRefreshJob> _logger;

    public AnalyticsRefreshJob(
        IMediator mediator,
        IAnalyticsDbContext db,
        IDbConnectionFactory connectionFactory,
        ILogger<AnalyticsRefreshJob> logger)
    {
        _mediator = mediator;
        _db = db;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters is not used here because we query via Dapper directly — no HTTP context means
        // no ICurrentTenant, so EF global filters would resolve TenantId to Guid.Empty and return nothing.
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        var activeSemesters = await conn.QueryAsync<SemesterEntry>(
            "SELECT Id, TenantId FROM semesters WHERE IsActive = 1 AND IsDeleted = 0");

        var list = activeSemesters.ToList();
        if (list.Count == 0)
        {
            _logger.LogInformation("AnalyticsRefreshJob: no active semesters found, skipping.");
            return;
        }

        var year = DateTime.UtcNow.Year;

        foreach (var semester in list)
        {
            _logger.LogInformation("AnalyticsRefreshJob: refreshing tenant {TenantId} semester {SemesterId}",
                semester.TenantId, semester.Id);

            await _mediator.Send(new ComputeAtRiskScoresCommand(semester.TenantId, semester.Id, year), cancellationToken);
            await _mediator.Send(new ComputeFeeDefaultRiskCommand(semester.TenantId, year), cancellationToken);
            await _mediator.Send(new ComputePlacementScoresCommand(semester.TenantId, year), cancellationToken);
        }
    }

    private class SemesterEntry
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
    }
}
