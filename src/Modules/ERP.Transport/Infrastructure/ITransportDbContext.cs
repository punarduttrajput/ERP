using ERP.Transport.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Infrastructure;

public interface ITransportDbContext
{
    DbSet<Route> Routes { get; }
    DbSet<RouteStop> RouteStops { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<RouteAssignment> RouteAssignments { get; }
    DbSet<GpsLocation> GpsLocations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
