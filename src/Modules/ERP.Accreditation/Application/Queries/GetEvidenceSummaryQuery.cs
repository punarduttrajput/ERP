using ERP.Accreditation.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Accreditation.Application.Queries;

public record GetEvidenceSummaryQuery(
    Guid TenantId,
    int AcademicYear,
    string? Module,
    string? Category
) : IRequest<IReadOnlyList<EvidenceSummaryDto>>;

public record EvidenceSummaryDto(
    Guid Id,
    int AcademicYear,
    string Module,
    string Category,
    string MetricKey,
    decimal? NumericValue,
    string? TextValue,
    DateTime ComputedAt
);

public class GetEvidenceSummaryHandler : IRequestHandler<GetEvidenceSummaryQuery, IReadOnlyList<EvidenceSummaryDto>>
{
    private readonly IAccreditationDbContext _db;

    public GetEvidenceSummaryHandler(IAccreditationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EvidenceSummaryDto>> Handle(GetEvidenceSummaryQuery request, CancellationToken cancellationToken)
    {
        var query = _db.EvidenceSummaries
            .Where(s => s.TenantId == request.TenantId
                     && s.AcademicYear == request.AcademicYear
                     && !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Module))
            query = query.Where(s => s.Module == request.Module);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(s => s.Category == request.Category);

        return await query
            .OrderBy(s => s.Module)
            .ThenBy(s => s.Category)
            .ThenBy(s => s.MetricKey)
            .Select(s => new EvidenceSummaryDto(
                s.Id,
                s.AcademicYear,
                s.Module,
                s.Category,
                s.MetricKey,
                s.NumericValue,
                s.TextValue,
                s.ComputedAt))
            .ToListAsync(cancellationToken);
    }
}
