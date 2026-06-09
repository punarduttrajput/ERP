using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Attendance.Application.Commands;

public record SubmitRegularizationCommand(
    Guid TenantId,
    Guid SessionId,
    Guid StudentId,
    string Reason) : IRequest<Result<Guid>>;

public class SubmitRegularizationHandler : IRequestHandler<SubmitRegularizationCommand, Result<Guid>>
{
    private readonly IAttendanceDbContext _db;

    public SubmitRegularizationHandler(IAttendanceDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(SubmitRegularizationCommand request, CancellationToken cancellationToken)
    {
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.SessionId == request.SessionId && r.StudentId == request.StudentId, cancellationToken);

        if (record is null)
            return Result<Guid>.Failure("Attendance record not found.");

        if (record.Status != AttendanceStatus.Absent)
            return Result<Guid>.Failure("Regularization can only be applied to absent records.");

        var existing = await _db.RegularizationRequests
            .AnyAsync(r => r.SessionId == request.SessionId && r.StudentId == request.StudentId && r.Status == RegularizationStatus.Pending, cancellationToken);

        if (existing)
            return Result<Guid>.Failure("A pending regularization request already exists for this session.");

        var req = new RegularizationRequest
        {
            TenantId = request.TenantId,
            SessionId = request.SessionId,
            StudentId = request.StudentId,
            Reason = request.Reason,
            Status = RegularizationStatus.Pending
        };

        _db.RegularizationRequests.Add(req);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(req.Id);
    }
}
