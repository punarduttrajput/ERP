using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Commands;

public record CreateRouteCommand(
    string Name,
    string? Description,
    Guid? VehicleId,
    Guid? DriverId,
    TimeOnly DepartureTime,
    TimeOnly ReturnTime
) : IRequest<Result<Guid>>;

public class CreateRouteCommandHandler : IRequestHandler<CreateRouteCommand, Result<Guid>>
{
    private readonly ITransportDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateRouteCommandHandler(ITransportDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        if (request.VehicleId.HasValue)
        {
            var vehicleExists = await _db.Vehicles.AnyAsync(v => v.Id == request.VehicleId.Value, cancellationToken);
            if (!vehicleExists)
                return Result<Guid>.Failure("Vehicle not found.");
        }

        if (request.DriverId.HasValue)
        {
            var driverExists = await _db.Drivers.AnyAsync(d => d.Id == request.DriverId.Value, cancellationToken);
            if (!driverExists)
                return Result<Guid>.Failure("Driver not found.");
        }

        var route = new Route
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            Name = request.Name,
            Description = request.Description,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            DepartureTime = request.DepartureTime,
            ReturnTime = request.ReturnTime
        };

        _db.Routes.Add(route);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(route.Id);
    }
}
