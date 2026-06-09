using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record RegisterForEventCommand(
    Guid TenantId,
    Guid EventId,
    Guid AlumniId
) : IRequest<Result<Guid>>;

public class RegisterForEventHandler : IRequestHandler<RegisterForEventCommand, Result<Guid>>
{
    private readonly IAlumniDbContext _db;

    public RegisterForEventHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(RegisterForEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _db.AlumniEvents
            .FirstOrDefaultAsync(x => x.Id == request.EventId && x.TenantId == request.TenantId, cancellationToken);

        if (ev is null)
            return Result.Failure<Guid>("Event not found.");

        if (!ev.IsPublished)
            return Result.Failure<Guid>("Event is not published yet.");

        if (ev.MaxParticipants.HasValue && ev.RegisteredCount >= ev.MaxParticipants.Value)
            return Result.Failure<Guid>("Event has reached maximum participant capacity.");

        var alreadyRegistered = await _db.EventRegistrations
            .AnyAsync(x => x.EventId == request.EventId && x.AlumniId == request.AlumniId && x.TenantId == request.TenantId, cancellationToken);

        if (alreadyRegistered)
            return Result.Failure<Guid>("Alumni is already registered for this event.");

        var registration = new EventRegistration
        {
            TenantId = request.TenantId,
            EventId = request.EventId,
            AlumniId = request.AlumniId,
            RegisteredAt = DateTime.UtcNow
        };

        _db.EventRegistrations.Add(registration);
        ev.RegisteredCount++;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(registration.Id);
    }
}
