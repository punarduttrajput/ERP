using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Attendance.Application.Commands;

public record SubmitQrAttendanceCommand(string QrToken, Guid StudentId) : IRequest<Result>;

public class SubmitQrAttendanceHandler : IRequestHandler<SubmitQrAttendanceCommand, Result>
{
    private readonly IAttendanceDbContext _db;

    public SubmitQrAttendanceHandler(IAttendanceDbContext db) => _db = db;

    public async Task<Result> Handle(SubmitQrAttendanceCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var session = await _db.AttendanceSessions
            .Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.QrToken == request.QrToken, cancellationToken);

        if (session is null)
            return Result.Failure("Invalid QR token.");

        if (session.QrExpiresAt <= now)
            return Result.Failure("QR expired.");

        if (session.IsLocked)
            return Result.Failure("Session locked.");

        var existing = session.Records.FirstOrDefault(r => r.StudentId == request.StudentId);
        if (existing is not null)
        {
            existing.Status = AttendanceStatus.Present;
            existing.MarkedAt = now;
            existing.MarkedBy = "QR";
        }
        else
        {
            session.Records.Add(new AttendanceRecord
            {
                TenantId = session.TenantId,
                SessionId = session.Id,
                StudentId = request.StudentId,
                Status = AttendanceStatus.Present,
                MarkedAt = now,
                MarkedBy = "QR"
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
