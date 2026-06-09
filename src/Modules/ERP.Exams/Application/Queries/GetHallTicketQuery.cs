using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Queries;

public record HallTicketDto(
    Guid StudentId,
    string RollNumber,
    string SeatNumber,
    Guid ExamScheduleId,
    string SubjectName,
    DateOnly ExamDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Venue);

public record GetHallTicketQuery(Guid StudentId, Guid ExamScheduleId) : IRequest<Result<HallTicketDto>>;

public class GetHallTicketHandler : IRequestHandler<GetHallTicketQuery, Result<HallTicketDto>>
{
    private readonly IExamsDbContext _db;

    public GetHallTicketHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<HallTicketDto>> Handle(GetHallTicketQuery request, CancellationToken cancellationToken)
    {
        var allocation = await _db.SeatAllocations
            .Include(s => s.ExamSchedule)
            .FirstOrDefaultAsync(s =>
                s.StudentId == request.StudentId &&
                s.ExamScheduleId == request.ExamScheduleId,
                cancellationToken);

        if (allocation is null)
            return Result<HallTicketDto>.Failure("Seat allocation not found for this student and exam schedule.");

        if (!allocation.IsEligible)
            return Result<HallTicketDto>.Failure(allocation.IneligibilityReason ?? "Student is not eligible for this exam.");

        var schedule = allocation.ExamSchedule!;

        // Mark hall ticket as generated
        allocation.HallTicketGenerated = true;
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new HallTicketDto(
            allocation.StudentId,
            allocation.RollNumber,
            allocation.SeatNumber,
            schedule.Id,
            schedule.SubjectName,
            schedule.ExamDate,
            schedule.StartTime,
            schedule.EndTime,
            schedule.Venue);

        return Result<HallTicketDto>.Success(dto);
    }
}
