using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Queries;

public record ExamScheduleDto(
    Guid Id,
    Guid SemesterId,
    Guid SubjectId,
    string SubjectName,
    DateOnly ExamDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Venue,
    int MaxMarks,
    int PassingMarks);

public record GetExamScheduleQuery(Guid SemesterId) : IRequest<Result<IReadOnlyList<ExamScheduleDto>>>;

public class GetExamScheduleHandler : IRequestHandler<GetExamScheduleQuery, Result<IReadOnlyList<ExamScheduleDto>>>
{
    private readonly IExamsDbContext _db;

    public GetExamScheduleHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ExamScheduleDto>>> Handle(GetExamScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedules = await _db.ExamSchedules
            .Where(s => s.SemesterId == request.SemesterId)
            .OrderBy(s => s.ExamDate)
            .ThenBy(s => s.StartTime)
            .Select(s => new ExamScheduleDto(
                s.Id,
                s.SemesterId,
                s.SubjectId,
                s.SubjectName,
                s.ExamDate,
                s.StartTime,
                s.EndTime,
                s.Venue,
                s.MaxMarks,
                s.PassingMarks))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ExamScheduleDto>>.Success(schedules);
    }
}
