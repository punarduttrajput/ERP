using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Commands;

public record CreateRoomCommand(
    Guid BlockId,
    string RoomNumber,
    int Floor,
    RoomType RoomType,
    int Capacity,
    decimal MonthlyRent
) : IRequest<Result<Guid>>;

public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, Result<Guid>>
{
    private readonly IHostelDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateRoomCommandHandler(IHostelDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var block = await _db.HostelBlocks.FirstOrDefaultAsync(b => b.Id == request.BlockId, cancellationToken);
        if (block is null)
            return Result<Guid>.Failure("Block not found.");

        var room = new HostelRoom
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            BlockId = request.BlockId,
            RoomNumber = request.RoomNumber,
            Floor = request.Floor,
            RoomType = request.RoomType,
            Capacity = request.Capacity,
            MonthlyRent = request.MonthlyRent,
            Status = RoomStatus.Available
        };

        _db.HostelRooms.Add(room);

        block.TotalRooms += 1;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(room.Id);
    }
}
