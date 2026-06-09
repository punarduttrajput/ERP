using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Commands;

public record AddRouteStopCommand(
    Guid RouteId,
    string Name,
    int Sequence,
    TimeOnly? PickupTime,
    decimal? DistanceFromCollegeKm
) : IRequest<Result<Guid>>;

public class AddRouteStopCommandHandler : IRequestHandler<AddRouteStopCommand, Result<Guid>>
{
    private readonly ITransportDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public AddRouteStopCommandHandler(ITransportDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(AddRouteStopCommand request, CancellationToken cancellationToken)
    {
        var route = await _db.Routes.FirstOrDefaultAsync(r => r.Id == request.RouteId, cancellationToken);
        if (route is null)
            return Result<Guid>.Failure("Route not found.");

        var duplicateSequence = await _db.RouteStops.AnyAsync(
            s => s.RouteId == request.RouteId && s.Sequence == request.Sequence, cancellationToken);
        if (duplicateSequence)
            return Result<Guid>.Failure($"Sequence {request.Sequence} already exists for this route.");

        var stop = new RouteStop
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            RouteId = request.RouteId,
            Name = request.Name,
            Sequence = request.Sequence,
            PickupTime = request.PickupTime,
            DistanceFromCollegeKm = request.DistanceFromCollegeKm
        };

        _db.RouteStops.Add(stop);

        route.TotalStops += 1;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(stop.Id);
    }
}
