using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Queries;

public record RoomDto(
    Guid Id,
    Guid BlockId,
    string BlockName,
    string RoomNumber,
    int Floor,
    RoomType RoomType,
    int Capacity,
    int OccupiedCount,
    RoomStatus Status,
    decimal MonthlyRent
);

public record GetRoomsQuery(
    Guid? BlockId,
    RoomStatus? Status,
    RoomType? RoomType
) : IRequest<Result<IReadOnlyList<RoomDto>>>;

public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, Result<IReadOnlyList<RoomDto>>>
{
    private readonly IHostelDbContext _db;

    public GetRoomsQueryHandler(IHostelDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<RoomDto>>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.HostelRooms
            .Include(r => r.Block)
            .Where(r => r.IsActive);

        if (request.BlockId.HasValue)
            query = query.Where(r => r.BlockId == request.BlockId.Value);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        if (request.RoomType.HasValue)
            query = query.Where(r => r.RoomType == request.RoomType.Value);

        var rooms = await query
            .OrderBy(r => r.Block!.Name)
            .ThenBy(r => r.Floor)
            .ThenBy(r => r.RoomNumber)
            .Select(r => new RoomDto(
                r.Id, r.BlockId, r.Block!.Name, r.RoomNumber, r.Floor,
                r.RoomType, r.Capacity, r.OccupiedCount, r.Status, r.MonthlyRent))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<RoomDto>>.Success(rooms);
    }
}
