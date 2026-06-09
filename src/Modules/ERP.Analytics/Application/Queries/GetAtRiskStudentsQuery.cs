using ERP.Analytics.Domain;
using ERP.Analytics.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Analytics.Application.Queries;

public record StudentRiskScoreDto(
    Guid StudentId,
    string StudentName,
    string ProgramName,
    int AcademicYear,
    decimal AttendancePercent,
    decimal AverageMarksPercent,
    decimal RiskScore,
    RiskLevel RiskLevel,
    bool AttendanceFlag,
    bool MarksFlag,
    bool CombinedFlag,
    DateTime ComputedAt
);

public record GetAtRiskStudentsQuery(
    RiskLevel? MinLevel,
    string? ProgramName,
    int? AcademicYear,
    int Page,
    int PageSize
) : IRequest<PagedResult<StudentRiskScoreDto>>;

public class GetAtRiskStudentsHandler : IRequestHandler<GetAtRiskStudentsQuery, PagedResult<StudentRiskScoreDto>>
{
    private readonly IAnalyticsDbContext _db;

    public GetAtRiskStudentsHandler(IAnalyticsDbContext db) => _db = db;

    public async Task<PagedResult<StudentRiskScoreDto>> Handle(GetAtRiskStudentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.StudentRiskScores.AsQueryable();

        if (request.MinLevel.HasValue)
            query = query.Where(x => x.RiskLevel >= request.MinLevel.Value);

        if (!string.IsNullOrWhiteSpace(request.ProgramName))
            query = query.Where(x => x.ProgramName == request.ProgramName);

        if (request.AcademicYear.HasValue)
            query = query.Where(x => x.AcademicYear == request.AcademicYear.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.RiskScore)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new StudentRiskScoreDto(
                x.StudentId, x.StudentName, x.ProgramName, x.AcademicYear,
                x.AttendancePercent, x.AverageMarksPercent, x.RiskScore, x.RiskLevel,
                x.AttendanceFlag, x.MarksFlag, x.CombinedFlag, x.ComputedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<StudentRiskScoreDto>(items, total, request.Page, request.PageSize);
    }
}
