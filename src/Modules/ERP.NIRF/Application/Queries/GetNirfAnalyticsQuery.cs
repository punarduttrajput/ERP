using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NIRF.Application.Queries;

public record NirfYearlyData(
    int Year,
    decimal? OverallScore,
    int? Rank,
    IDictionary<string, decimal> ParameterScores);

public record NirfAnalyticsDto(
    IReadOnlyList<NirfYearlyData> YearlyData,
    IReadOnlyList<string> Parameters,
    decimal? BestRank,
    int? BestRankYear);

public record GetNirfAnalyticsQuery(Guid TenantId, int Years = 5) : IRequest<Result<NirfAnalyticsDto>>;

public class GetNirfAnalyticsHandler : IRequestHandler<GetNirfAnalyticsQuery, Result<NirfAnalyticsDto>>
{
    private readonly INirfDbContext _db;

    public GetNirfAnalyticsHandler(INirfDbContext db) => _db = db;

    public Task<Result<NirfAnalyticsDto>> Handle(GetNirfAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var fromYear = currentYear - request.Years + 1;

        var submissions = _db.NirfSubmissions
            .Include(s => s.ParameterScores.Where(p => !p.IsDeleted))
            .Where(s => s.TenantId == request.TenantId && s.RankingYear >= fromYear && !s.IsDeleted)
            .OrderBy(s => s.RankingYear)
            .ToList();

        var rankHistory = _db.NirfRankHistory
            .Where(r => r.TenantId == request.TenantId && r.RankingYear >= fromYear && !r.IsDeleted)
            .ToList();

        var yearlyData = submissions.Select(s =>
        {
            var rank = rankHistory.FirstOrDefault(r => r.RankingYear == s.RankingYear && r.Category == s.Category)?.Rank;
            var paramScores = s.ParameterScores.ToDictionary(p => p.Parameter, p => p.RawScore);
            return new NirfYearlyData(s.RankingYear, s.OverallScore, rank, paramScores);
        }).ToList();

        var allRanks = yearlyData.Where(y => y.Rank.HasValue).ToList();
        // Lowest rank number = best rank in NIRF
        var best = allRanks.Count > 0 ? allRanks.MinBy(y => y.Rank!.Value) : null;

        var dto = new NirfAnalyticsDto(
            yearlyData,
            Domain.NirfParameter.All,
            best?.Rank,
            best?.Year);

        return Task.FromResult(Result.Success(dto));
    }
}
