using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Attendance.Application.Commands;

public record CreateSessionCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid BatchId,
    Guid SemesterId,
    Guid FacultyUserId,
    DateOnly SessionDate,
    int PeriodNumber,
    IReadOnlyList<Guid> StudentIds) : IRequest<Result<Guid>>;

public class CreateSessionHandler : IRequestHandler<CreateSessionCommand, Result<Guid>>
{
    private readonly IAttendanceDbContext _db;

    public CreateSessionHandler(IAttendanceDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new AttendanceSession
        {
            TenantId = request.TenantId,
            SubjectId = request.SubjectId,
            BatchId = request.BatchId,
            SemesterId = request.SemesterId,
            FacultyUserId = request.FacultyUserId,
            SessionDate = request.SessionDate,
            PeriodNumber = request.PeriodNumber
        };

        var now = DateTime.UtcNow;

        // Pre-populate all enrolled students as Absent; faculty only changes present ones
        foreach (var studentId in request.StudentIds)
        {
            session.Records.Add(new AttendanceRecord
            {
                TenantId = request.TenantId,
                SessionId = session.Id,
                StudentId = studentId,
                Status = AttendanceStatus.Absent,
                MarkedAt = now,
                MarkedBy = "Faculty"
            });
        }

        _db.AttendanceSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(session.Id);
    }
}
