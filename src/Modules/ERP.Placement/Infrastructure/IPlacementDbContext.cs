using ERP.Placement.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Infrastructure;

public interface IPlacementDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<PlacementDrive> Drives { get; }
    DbSet<DriveRegistration> Registrations { get; }
    DbSet<PlacementOffer> Offers { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
