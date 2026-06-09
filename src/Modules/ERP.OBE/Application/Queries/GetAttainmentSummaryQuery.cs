using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Queries;

public record GetAttainmentSummaryQuery(Guid TenantId, Guid SubjectId, Guid SemesterId)
    : IRequest<Result<AttainmentSummaryDto>>;

public record CoAttainmentSummaryDto(
    string CourseOutcomeCode,
    decimal DirectAttainmentPercent,
    AttainmentLevel DirectLevel,
    decimal? IndirectAttainmentPercent,
    decimal? CombinedAttainmentPercent,
    decimal? GapPercent);

public record AttainmentSummaryDto(
    Guid SubjectId,
    Guid SemesterId,
    IReadOnlyList<CoAttainmentSummaryDto> CourseOutcomes);

public class GetAttainmentSummaryHandler : IRequestHandler<GetAttainmentSummaryQuery, Result<AttainmentSummaryDto>>
{
    private readonly IObeDbContext _db;

    public GetAttainmentSummaryHandler(IObeDbContext db) => _db = db;

    public async Task<Result<AttainmentSummaryDto>> Handle(GetAttainmentSummaryQuery request, CancellationToken cancellationToken)
    {
        var directs = await _db.DirectAttainments
            .Where(x => x.TenantId == request.TenantId
                     && x.SubjectId == request.SubjectId
                     && x.SemesterId == request.SemesterId
                     && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var gaps = await _db.AttainmentGaps
            .Where(x => x.TenantId == request.TenantId
                     && x.SubjectId == request.SubjectId
                     && x.SemesterId == request.SemesterId
                     && !x.IsDeleted)
            .ToDictionaryAsync(x => x.CourseOutcomeCode, cancellationToken);

        var items = directs.Select(d =>
        {
            gaps.TryGetValue(d.CourseOutcomeCode, out var gap);
            return new CoAttainmentSummaryDto(
                d.CourseOutcomeCode,
                d.AttainmentPercent,
                d.Level,
                gap?.IndirectAttainmentPercent,
                gap?.CombinedAttainmentPercent,
                gap?.GapPercent);
        }).ToList();

        return Result<AttainmentSummaryDto>.Success(new AttainmentSummaryDto(request.SubjectId, request.SemesterId, items));
    }
}
