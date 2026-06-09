using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Queries;

public record WaitlistEntryDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    RoomType PreferredRoomType,
    Guid? PreferredBlockId,
    int AcademicYear,
    DateTime RequestedAt,
    int Priority
);

public record GetWaitlistQuery(Guid? BlockId, RoomType? RoomType) : IRequest<Result<IReadOnlyList<WaitlistEntryDto>>>;

public class GetWaitlistQueryHandler : IRequestHandler<GetWaitlistQuery, Result<IReadOnlyList<WaitlistEntryDto>>>
{
    private readonly IHostelDbContext _db;

    public GetWaitlistQueryHandler(IHostelDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<WaitlistEntryDto>>> Handle(GetWaitlistQuery request, CancellationToken cancellationToken)
    {
        var query = _db.HostelWaitlist.Where(w => !w.IsPromoted);

        if (request.BlockId.HasValue)
            query = query.Where(w => w.PreferredBlockId == request.BlockId.Value);

        if (request.RoomType.HasValue)
            query = query.Where(w => w.PreferredRoomType == request.RoomType.Value);

        var entries = await query
            .OrderBy(w => w.Priority)
            .Select(w => new WaitlistEntryDto(
                w.Id, w.StudentId, w.StudentName, w.PreferredRoomType,
                w.PreferredBlockId, w.AcademicYear, w.RequestedAt, w.Priority))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<WaitlistEntryDto>>.Success(entries);
    }
}
