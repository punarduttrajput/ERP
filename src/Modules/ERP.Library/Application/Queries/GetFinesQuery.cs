using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Queries;

public record FineDto(
    Guid Id,
    Guid IssueId,
    Guid MemberId,
    int DaysOverdue,
    decimal FinePerDay,
    decimal TotalFine,
    FineStatus Status,
    DateTime? CollectedAt,
    Guid? WaivedBy,
    string? WaivedReason,
    DateTime CreatedAt
);

public record GetFinesQuery(
    Guid? MemberId,
    FineStatus? Status
) : IRequest<IReadOnlyList<FineDto>>;

public class GetFinesQueryHandler : IRequestHandler<GetFinesQuery, IReadOnlyList<FineDto>>
{
    private readonly ILibraryDbContext _db;

    public GetFinesQueryHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FineDto>> Handle(GetFinesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.LibraryFines.Where(x => !x.IsDeleted);

        if (request.MemberId.HasValue)
            query = query.Where(x => x.MemberId == request.MemberId.Value);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        var fines = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

        return fines.Select(x => new FineDto(
            x.Id, x.IssueId, x.MemberId, x.DaysOverdue, x.FinePerDay,
            x.TotalFine, x.Status, x.CollectedAt, x.WaivedBy, x.WaivedReason, x.CreatedAt
        )).ToList();
    }
}
