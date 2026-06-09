using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Alumni.Application.Commands;

public record CreateEventCommand(
    Guid TenantId,
    Guid OrganisedBy,
    string Title,
    string? Description,
    EventType EventType,
    DateOnly EventDate,
    TimeOnly? EventTime,
    string? VenueOrLink,
    int? MaxParticipants
) : IRequest<Result<Guid>>;

public class CreateEventHandler : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    private readonly IAlumniDbContext _db;

    public CreateEventHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var ev = new AlumniEvent
        {
            TenantId = request.TenantId,
            OrganisedBy = request.OrganisedBy,
            Title = request.Title,
            Description = request.Description,
            EventType = request.EventType,
            EventDate = request.EventDate,
            EventTime = request.EventTime,
            VenueOrLink = request.VenueOrLink,
            MaxParticipants = request.MaxParticipants,
            IsPublished = false
        };

        _db.AlumniEvents.Add(ev);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ev.Id);
    }
}
