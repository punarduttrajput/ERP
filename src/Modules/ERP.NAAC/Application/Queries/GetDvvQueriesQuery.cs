using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Queries;

public record GetDvvQueriesQuery(Guid? SsrId, DvvStatus? Status) : IRequest<Result<IReadOnlyList<DvvQueryDto>>>;

public record DvvQueryDto(
    Guid Id,
    Guid SsrId,
    string QueryNumber,
    string CriterionNumber,
    string IndicatorNumber,
    string QueryText,
    string? Response,
    DvvStatus Status,
    DateTime ReceivedAt,
    DateTime? RespondedAt);

public class GetDvvQueriesHandler : IRequestHandler<GetDvvQueriesQuery, Result<IReadOnlyList<DvvQueryDto>>>
{
    private readonly INaacDbContext _db;

    public GetDvvQueriesHandler(INaacDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<DvvQueryDto>>> Handle(GetDvvQueriesQuery request, CancellationToken cancellationToken)
    {
        var q = _db.DvvQueries.AsQueryable();

        if (request.SsrId.HasValue)
            q = q.Where(d => d.SsrId == request.SsrId.Value);

        if (request.Status.HasValue)
            q = q.Where(d => d.Status == request.Status.Value);

        var list = await q.OrderBy(d => d.ReceivedAt)
            .Select(d => new DvvQueryDto(
                d.Id, d.SsrId, d.QueryNumber, d.CriterionNumber, d.IndicatorNumber,
                d.QueryText, d.Response, d.Status, d.ReceivedAt, d.RespondedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<DvvQueryDto>>.Success(list);
    }
}
