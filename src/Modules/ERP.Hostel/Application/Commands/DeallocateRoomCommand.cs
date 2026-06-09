using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Commands;

public record DeallocateRoomCommand(Guid AllocationId) : IRequest<Result>;

public class DeallocateRoomCommandHandler : IRequestHandler<DeallocateRoomCommand, Result>
{
    private readonly IHostelDbContext _db;
    private readonly IMediator _mediator;

    public DeallocateRoomCommandHandler(IHostelDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Result> Handle(DeallocateRoomCommand request, CancellationToken cancellationToken)
    {
        var allocation = await _db.RoomAllocations
            .Include(a => a.Room)
                .ThenInclude(r => r!.Block)
            .FirstOrDefaultAsync(a => a.Id == request.AllocationId && a.Status == AllocationStatus.Active, cancellationToken);

        if (allocation is null)
            return Result.Failure("Active allocation not found.");

        var room = allocation.Room!;
        var wasFullyOccupied = room.Status == RoomStatus.FullyOccupied;

        allocation.VacatedAt = DateTime.UtcNow;
        allocation.Status = AllocationStatus.Vacated;

        room.OccupiedCount -= 1;
        var newStatus = AllocateRoomCommandHandler.ComputeStatus(room.OccupiedCount, room.Capacity);

        // Decrement block counter only when transitioning away from FullyOccupied
        if (wasFullyOccupied && newStatus != RoomStatus.FullyOccupied && room.Block is not null)
            room.Block.OccupiedRooms -= 1;

        room.Status = newStatus;

        await _db.SaveChangesAsync(cancellationToken);

        // Promote highest-priority waitlist entry matching this block and room type
        var waitlistEntry = await _db.HostelWaitlist
            .Where(w => !w.IsPromoted
                && w.PreferredRoomType == room.RoomType
                && (w.PreferredBlockId == null || w.PreferredBlockId == room.BlockId))
            .OrderBy(w => w.Priority)
            .FirstOrDefaultAsync(cancellationToken);

        if (waitlistEntry is not null)
        {
            // Keeping allocation logic centralised in AllocateRoomCommand
            await _mediator.Send(new AllocateRoomCommand(
                room.Id,
                waitlistEntry.StudentId,
                waitlistEntry.StudentName,
                waitlistEntry.AcademicYear), cancellationToken);

            waitlistEntry.IsPromoted = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
