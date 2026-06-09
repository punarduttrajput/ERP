using ERP.Analytics.Domain;
using ERP.Analytics.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Analytics.Application.Queries;

public record PlacementScoreDto(
    Guid StudentId,
    string StudentName,
    string ProgramName,
    int AcademicYear,
    decimal Cgpa,
    int ActiveBacklogs,
    decimal AttendancePercent,
    decimal PlacementScore,
    decimal PlacementProbabilityPercent,
    DateTime ComputedAt
);

public record GetPlacementScoresQuery(
    int? AcademicYear,
    string? ProgramName,
    int Page,
    int PageSize
) : IRequest<PagedResult<PlacementScoreDto>>;

public class GetPlacementScoresHandler : IRequestHandler<GetPlacementScoresQuery, PagedResult<PlacementScoreDto>>
{
    private readonly IAnalyticsDbContext _db;

    public GetPlacementScoresHandler(IAnalyticsDbContext db) => _db = db;

    public async Task<PagedResult<PlacementScoreDto>> Handle(GetPlacementScoresQuery request, CancellationToken cancellationToken)
    {
        var query = _db.PlacementScores.AsQueryable();

        if (request.AcademicYear.HasValue)
            query = query.Where(x => x.AcademicYear == request.AcademicYear.Value);

        if (!string.IsNullOrWhiteSpace(request.ProgramName))
            query = query.Where(x => x.ProgramName == request.ProgramName);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.PlacementScoreValue)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PlacementScoreDto(
                x.StudentId, x.StudentName, x.ProgramName, x.AcademicYear,
                x.Cgpa, x.ActiveBacklogs, x.AttendancePercent,
                x.PlacementScoreValue, x.PlacementProbabilityPercent, x.ComputedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlacementScoreDto>(items, total, request.Page, request.PageSize);
    }
}
