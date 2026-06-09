using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Attendance.Application.Commands;

public record SubmitBiometricLogCommand(
    Guid TenantId,
    string DeviceId,
    string BiometricId,
    DateTime LoggedAt) : IRequest<Result>;

public class SubmitBiometricLogHandler : IRequestHandler<SubmitBiometricLogCommand, Result>
{
    private readonly IAttendanceDbContext _db;

    public SubmitBiometricLogHandler(IAttendanceDbContext db) => _db = db;

    public async Task<Result> Handle(SubmitBiometricLogCommand request, CancellationToken cancellationToken)
    {
        var log = new BiometricLog
        {
            TenantId = request.TenantId,
            DeviceId = request.DeviceId,
            BiometricId = request.BiometricId,
            LoggedAt = request.LoggedAt,
            IsProcessed = false
            // StudentId is resolved by a downstream processor that has the BiometricId→StudentId mapping
        };

        _db.BiometricLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
