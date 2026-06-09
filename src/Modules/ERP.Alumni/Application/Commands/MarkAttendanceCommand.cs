using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record MarkAttendanceCommand(Guid TenantId, Guid EventId, Guid AlumniId) : IRequest<Result>;

public class MarkAttendanceHandler : IRequestHandler<MarkAttendanceCommand, Result>
{
    private readonly IAlumniDbContext _db;

    public MarkAttendanceHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result> Handle(MarkAttendanceCommand request, CancellationToken cancellationToken)
    {
        var registration = await _db.EventRegistrations
            .FirstOrDefaultAsync(x => x.EventId == request.EventId && x.AlumniId == request.AlumniId && x.TenantId == request.TenantId, cancellationToken);

        if (registration is null)
            return Result.Failure("Registration not found for this alumni and event.");

        registration.AttendedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
