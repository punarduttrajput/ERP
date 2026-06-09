using ERP.Analytics.Domain;
using ERP.Analytics.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Analytics.Application.Queries;

public record FeeDefaultRiskScoreDto(
    Guid StudentId,
    string StudentName,
    int AcademicYear,
    decimal TotalDue,
    int OverdueDays,
    int PreviousDefaultCount,
    decimal RiskScore,
    RiskLevel RiskLevel,
    DateTime ComputedAt
);

public record GetFeeDefaultRiskQuery(
    RiskLevel? MinLevel,
    int? AcademicYear,
    int Page,
    int PageSize
) : IRequest<PagedResult<FeeDefaultRiskScoreDto>>;

public class GetFeeDefaultRiskHandler : IRequestHandler<GetFeeDefaultRiskQuery, PagedResult<FeeDefaultRiskScoreDto>>
{
    private readonly IAnalyticsDbContext _db;

    public GetFeeDefaultRiskHandler(IAnalyticsDbContext db) => _db = db;

    public async Task<PagedResult<FeeDefaultRiskScoreDto>> Handle(GetFeeDefaultRiskQuery request, CancellationToken cancellationToken)
    {
        var query = _db.FeeDefaultRiskScores.AsQueryable();

        if (request.MinLevel.HasValue)
            query = query.Where(x => x.RiskLevel >= request.MinLevel.Value);

        if (request.AcademicYear.HasValue)
            query = query.Where(x => x.AcademicYear == request.AcademicYear.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.RiskScore)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new FeeDefaultRiskScoreDto(
                x.StudentId, x.StudentName, x.AcademicYear,
                x.TotalDue, x.OverdueDays, x.PreviousDefaultCount,
                x.RiskScore, x.RiskLevel, x.ComputedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<FeeDefaultRiskScoreDto>(items, total, request.Page, request.PageSize);
    }
}
