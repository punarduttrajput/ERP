using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Attendance.Application.Commands;

public record ReviewRegularizationCommand(
    Guid RequestId,
    Guid ReviewerUserId,
    bool Approved,
    string? Remark) : IRequest<Result>;

public class ReviewRegularizationHandler : IRequestHandler<ReviewRegularizationCommand, Result>
{
    private readonly IAttendanceDbContext _db;

    public ReviewRegularizationHandler(IAttendanceDbContext db) => _db = db;

    public async Task<Result> Handle(ReviewRegularizationCommand request, CancellationToken cancellationToken)
    {
        var req = await _db.RegularizationRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (req is null)
            return Result.Failure("Regularization request not found.");

        if (req.Status != RegularizationStatus.Pending)
            return Result.Failure("Request has already been reviewed.");

        var now = DateTime.UtcNow;

        if (request.Approved)
        {
            var record = await _db.AttendanceRecords
                .FirstOrDefaultAsync(r => r.SessionId == req.SessionId && r.StudentId == req.StudentId, cancellationToken);

            if (record is not null)
            {
                record.Status = AttendanceStatus.Present;
                record.MarkedAt = now;
                record.MarkedBy = "Regularization";
            }

            // Temporarily unlock the session to allow status update, then re-lock
            var session = await _db.AttendanceSessions
                .FirstOrDefaultAsync(s => s.Id == req.SessionId, cancellationToken);

            if (session is not null)
                session.IsLocked = true;

            req.Status = RegularizationStatus.Approved;
        }
        else
        {
            req.Status = RegularizationStatus.Rejected;
        }

        req.ReviewedBy = request.ReviewerUserId;
        req.ReviewedAt = now;
        req.ReviewRemark = request.Remark;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
