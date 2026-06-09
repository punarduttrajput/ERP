using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Exams.Application.Commands;

public record CreateExamScheduleCommand(
    Guid TenantId,
    Guid SemesterId,
    Guid SubjectId,
    string SubjectName,
    DateOnly ExamDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Venue,
    int MaxMarks,
    int PassingMarks) : IRequest<Result<Guid>>;

public class CreateExamScheduleHandler : IRequestHandler<CreateExamScheduleCommand, Result<Guid>>
{
    private readonly IExamsDbContext _db;

    public CreateExamScheduleHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateExamScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = new ExamSchedule
        {
            TenantId = request.TenantId,
            SemesterId = request.SemesterId,
            SubjectId = request.SubjectId,
            SubjectName = request.SubjectName,
            ExamDate = request.ExamDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Venue = request.Venue,
            MaxMarks = request.MaxMarks,
            PassingMarks = request.PassingMarks
        };

        _db.ExamSchedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(schedule.Id);
    }
}
