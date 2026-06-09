using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Queries;

public record AllocationDto(
    Guid AllocationId,
    Guid RoomId,
    string RoomNumber,
    string BlockName,
    int Floor,
    RoomType RoomType,
    decimal MonthlyRent,
    DateTime AllocatedAt,
    int AcademicYear
);

public record GetAllocationQuery(Guid StudentId) : IRequest<Result<AllocationDto?>>;

public class GetAllocationQueryHandler : IRequestHandler<GetAllocationQuery, Result<AllocationDto?>>
{
    private readonly IHostelDbContext _db;

    public GetAllocationQueryHandler(IHostelDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AllocationDto?>> Handle(GetAllocationQuery request, CancellationToken cancellationToken)
    {
        var allocation = await _db.RoomAllocations
            .Include(a => a.Room)
                .ThenInclude(r => r!.Block)
            .Where(a => a.StudentId == request.StudentId && a.Status == AllocationStatus.Active)
            .Select(a => new AllocationDto(
                a.Id,
                a.RoomId,
                a.Room!.RoomNumber,
                a.Room.Block!.Name,
                a.Room.Floor,
                a.Room.RoomType,
                a.Room.MonthlyRent,
                a.AllocatedAt,
                a.AcademicYear))
            .FirstOrDefaultAsync(cancellationToken);

        return Result<AllocationDto?>.Success(allocation);
    }
}
