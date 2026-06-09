using ERP.Tenants.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Tenants.Application.Queries;

public interface ITenantsReadDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantSettings> TenantSettings { get; }
}
