using ERP.Tenants.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Tenants.Application.Commands;

public interface ITenantsDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantSettings> TenantSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
