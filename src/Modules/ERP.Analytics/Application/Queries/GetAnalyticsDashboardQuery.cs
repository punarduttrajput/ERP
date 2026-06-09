using ERP.Analytics.Domain;
using ERP.Analytics.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Analytics.Application.Queries;

public record AnalyticsDashboardDto(
    int TotalStudentsAnalysed,
    int CriticalRiskStudents,
    int HighRiskStudents,
    int FeeDefaultRiskCount,
    decimal AveragePlacementScore,
    int HighPlacementProbabilityCount,
    DateTime? LastComputedAt
);

public record GetAnalyticsDashboardQuery : IRequest<AnalyticsDashboardDto>;

public class GetAnalyticsDashboardHandler : IRequestHandler<GetAnalyticsDashboardQuery, AnalyticsDashboardDto>
{
    private readonly IAnalyticsDbContext _db;

    public GetAnalyticsDashboardHandler(IAnalyticsDbContext db) => _db = db;

    public async Task<AnalyticsDashboardDto> Handle(GetAnalyticsDashboardQuery request, CancellationToken cancellationToken)
    {
        var totalStudents = await _db.StudentRiskScores.CountAsync(cancellationToken);
        var criticalCount = await _db.StudentRiskScores.CountAsync(x => x.RiskLevel == RiskLevel.Critical, cancellationToken);
        var highCount = await _db.StudentRiskScores.CountAsync(x => x.RiskLevel == RiskLevel.High, cancellationToken);
        var feeRiskCount = await _db.FeeDefaultRiskScores.CountAsync(x => x.RiskLevel >= RiskLevel.High, cancellationToken);

        var avgPlacement = await _db.PlacementScores.AnyAsync(cancellationToken)
            ? await _db.PlacementScores.AverageAsync(x => x.PlacementScoreValue, cancellationToken)
            : 0m;

        var highPlacement = await _db.PlacementScores.CountAsync(x => x.PlacementProbabilityPercent >= 65m, cancellationToken);

        var lastComputed = await _db.StudentRiskScores
            .OrderByDescending(x => x.ComputedAt)
            .Select(x => (DateTime?)x.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new AnalyticsDashboardDto(
            totalStudents, criticalCount, highCount,
            feeRiskCount, avgPlacement, highPlacement, lastComputed);
    }
}
