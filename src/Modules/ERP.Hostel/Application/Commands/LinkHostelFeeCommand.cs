using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Commands;

public record HostelFeeDto(Guid RoomId, string RoomNumber, decimal MonthlyRent, int AcademicYear);

public record LinkHostelFeeCommand(
    Guid RoomId,
    Guid StudentId,
    int AcademicYear,
    int SemesterNumber
) : IRequest<Result<HostelFeeDto>>;

public class LinkHostelFeeCommandHandler : IRequestHandler<LinkHostelFeeCommand, Result<HostelFeeDto>>
{
    private readonly IHostelDbContext _db;

    public LinkHostelFeeCommandHandler(IHostelDbContext db)
    {
        _db = db;
    }

    public async Task<Result<HostelFeeDto>> Handle(LinkHostelFeeCommand request, CancellationToken cancellationToken)
    {
        var room = await _db.HostelRooms
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

        if (room is null)
            return Result<HostelFeeDto>.Failure("Room not found.");

        // Returns rent data for the Fee module to create the invoice — no fee records are created here
        var dto = new HostelFeeDto(room.Id, room.RoomNumber, room.MonthlyRent, request.AcademicYear);

        return Result<HostelFeeDto>.Success(dto);
    }
}
