using ERP.Shared.Application.Common;
using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Commands;

public record RemoveRouteAssignmentCommand(Guid AssignmentId) : IRequest<Result>;

public class RemoveRouteAssignmentCommandHandler : IRequestHandler<RemoveRouteAssignmentCommand, Result>
{
    private readonly ITransportDbContext _db;

    public RemoveRouteAssignmentCommandHandler(ITransportDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(RemoveRouteAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _db.RouteAssignments
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);
        if (assignment is null)
            return Result.Failure("Assignment not found.");

        var route = await _db.Routes.FirstOrDefaultAsync(r => r.Id == assignment.RouteId, cancellationToken);

        assignment.IsActive = false;
        assignment.IsDeleted = true;

        if (route is not null && route.TotalPassengers > 0)
            route.TotalPassengers -= 1;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
