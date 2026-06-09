using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Attendance.Application.Queries;

public record SessionAttendanceDto(
    Guid SessionId,
    Guid SubjectId,
    Guid BatchId,
    DateOnly SessionDate,
    int PeriodNumber,
    bool IsLocked,
    IReadOnlyList<AttendanceRecordDto> Records);

public record AttendanceRecordDto(
    Guid StudentId,
    AttendanceStatus Status,
    DateTime MarkedAt,
    string MarkedBy);

public record GetSessionAttendanceQuery(Guid SessionId) : IRequest<Result<SessionAttendanceDto>>;

public class GetSessionAttendanceHandler : IRequestHandler<GetSessionAttendanceQuery, Result<SessionAttendanceDto>>
{
    private readonly IAttendanceDbContext _db;

    public GetSessionAttendanceHandler(IAttendanceDbContext db) => _db = db;

    public async Task<Result<SessionAttendanceDto>> Handle(GetSessionAttendanceQuery request, CancellationToken cancellationToken)
    {
        var session = await _db.AttendanceSessions
            .Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            return Result<SessionAttendanceDto>.Failure("Session not found.");

        var dto = new SessionAttendanceDto(
            session.Id,
            session.SubjectId,
            session.BatchId,
            session.SessionDate,
            session.PeriodNumber,
            session.IsLocked,
            session.Records.Select(r => new AttendanceRecordDto(r.StudentId, r.Status, r.MarkedAt, r.MarkedBy)).ToList());

        return Result<SessionAttendanceDto>.Success(dto);
    }
}
