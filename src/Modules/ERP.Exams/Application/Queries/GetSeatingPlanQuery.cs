using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Queries;

public record SeatAllocationDto(
    Guid StudentId,
    string RollNumber,
    string SeatNumber,
    bool IsEligible,
    string? IneligibilityReason,
    bool HallTicketGenerated);

public record GetSeatingPlanQuery(Guid ExamScheduleId) : IRequest<Result<IReadOnlyList<SeatAllocationDto>>>;

public class GetSeatingPlanHandler : IRequestHandler<GetSeatingPlanQuery, Result<IReadOnlyList<SeatAllocationDto>>>
{
    private readonly IExamsDbContext _db;

    public GetSeatingPlanHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<SeatAllocationDto>>> Handle(GetSeatingPlanQuery request, CancellationToken cancellationToken)
    {
        var allocations = await _db.SeatAllocations
            .Where(s => s.ExamScheduleId == request.ExamScheduleId)
            .OrderBy(s => s.SeatNumber)
            .Select(s => new SeatAllocationDto(
                s.StudentId,
                s.RollNumber,
                s.SeatNumber,
                s.IsEligible,
                s.IneligibilityReason,
                s.HallTicketGenerated))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SeatAllocationDto>>.Success(allocations);
    }
}
