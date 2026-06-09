using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record PublishEventCommand(Guid TenantId, Guid EventId) : IRequest<Result>;

public class PublishEventHandler : IRequestHandler<PublishEventCommand, Result>
{
    private readonly IAlumniDbContext _db;

    public PublishEventHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _db.AlumniEvents
            .FirstOrDefaultAsync(x => x.Id == request.EventId && x.TenantId == request.TenantId, cancellationToken);

        if (ev is null)
            return Result.Failure("Event not found.");

        ev.IsPublished = true;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
