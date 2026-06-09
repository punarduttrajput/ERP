using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Queries;

public record VisitorLogDto(
    Guid Id,
    string VisitorName,
    string VisitorMobile,
    string VisitorIdType,
    string VisitorIdNumber,
    Guid StudentId,
    string StudentName,
    Guid BlockId,
    string PurposeOfVisit,
    DateTime CheckInAt,
    DateTime? CheckOutAt,
    Guid CheckedInBy
);

public record GetVisitorLogQuery(Guid? BlockId, DateOnly? Date) : IRequest<Result<IReadOnlyList<VisitorLogDto>>>;

public class GetVisitorLogQueryHandler : IRequestHandler<GetVisitorLogQuery, Result<IReadOnlyList<VisitorLogDto>>>
{
    private readonly IHostelDbContext _db;

    public GetVisitorLogQueryHandler(IHostelDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<VisitorLogDto>>> Handle(GetVisitorLogQuery request, CancellationToken cancellationToken)
    {
        var query = _db.VisitorEntries.AsQueryable();

        if (request.BlockId.HasValue)
            query = query.Where(v => v.BlockId == request.BlockId.Value);

        if (request.Date.HasValue)
        {
            var start = request.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = start.AddDays(1);
            query = query.Where(v => v.CheckInAt >= start && v.CheckInAt < end);
        }

        var entries = await query
            .OrderByDescending(v => v.CheckInAt)
            .Select(v => new VisitorLogDto(
                v.Id, v.VisitorName, v.VisitorMobile, v.VisitorIdType, v.VisitorIdNumber,
                v.StudentId, v.StudentName, v.BlockId, v.PurposeOfVisit,
                v.CheckInAt, v.CheckOutAt, v.CheckedInBy))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<VisitorLogDto>>.Success(entries);
    }
}
