using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Attendance.Application.Commands;

public record AttendanceMark(Guid StudentId, AttendanceStatus Status);

public record MarkAttendanceCommand(
    Guid TenantId,
    Guid SessionId,
    IReadOnlyList<AttendanceMark> Marks) : IRequest<Result>;

public class MarkAttendanceHandler : IRequestHandler<MarkAttendanceCommand, Result>
{
    private readonly IAttendanceDbContext _db;

    public MarkAttendanceHandler(IAttendanceDbContext db) => _db = db;

    public async Task<Result> Handle(MarkAttendanceCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.AttendanceSessions
            .Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure("Session not found.");

        if (session.IsLocked)
            return Result.Failure("Session is already locked.");

        var now = DateTime.UtcNow;
        var existingByStudent = session.Records.ToDictionary(r => r.StudentId);

        foreach (var mark in request.Marks)
        {
            if (existingByStudent.TryGetValue(mark.StudentId, out var record))
            {
                record.Status = mark.Status;
                record.MarkedAt = now;
                record.MarkedBy = "Faculty";
            }
            else
            {
                session.Records.Add(new AttendanceRecord
                {
                    TenantId = request.TenantId,
                    SessionId = session.Id,
                    StudentId = mark.StudentId,
                    Status = mark.Status,
                    MarkedAt = now,
                    MarkedBy = "Faculty"
                });
            }
        }

        session.IsLocked = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
