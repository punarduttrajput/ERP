using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Queries;

public record AlumniEventDto(
    Guid Id,
    string Title,
    string? Description,
    EventType EventType,
    DateOnly EventDate,
    TimeOnly? EventTime,
    string? VenueOrLink,
    int? MaxParticipants,
    int RegisteredCount,
    bool IsPublished,
    Guid OrganisedBy
);

public record GetEventsQuery(
    Guid TenantId,
    EventType? EventType,
    bool? UpcomingOnly
) : IRequest<Result<IReadOnlyList<AlumniEventDto>>>;

public class GetEventsHandler : IRequestHandler<GetEventsQuery, Result<IReadOnlyList<AlumniEventDto>>>
{
    private readonly IAlumniDbContext _db;

    public GetEventsHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<AlumniEventDto>>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AlumniEvents.Where(x => x.TenantId == request.TenantId && x.IsPublished);

        if (request.EventType.HasValue)
            query = query.Where(x => x.EventType == request.EventType.Value);

        if (request.UpcomingOnly == true)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(x => x.EventDate >= today);
        }

        var events = await query
            .OrderBy(x => x.EventDate)
            .Select(x => new AlumniEventDto(
                x.Id, x.Title, x.Description, x.EventType, x.EventDate, x.EventTime,
                x.VenueOrLink, x.MaxParticipants, x.RegisteredCount, x.IsPublished, x.OrganisedBy))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AlumniEventDto>>(events);
    }
}
