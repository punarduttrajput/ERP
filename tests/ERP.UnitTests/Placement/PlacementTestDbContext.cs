using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.UnitTests.Placement;

public class PlacementTestDbContext : DbContext, IPlacementDbContext
{
    public PlacementTestDbContext(DbContextOptions<PlacementTestDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PlacementDrive> Drives => Set<PlacementDrive>();
    public DbSet<DriveRegistration> Registrations => Set<DriveRegistration>();
    public DbSet<PlacementOffer> Offers => Set<PlacementOffer>();
}
