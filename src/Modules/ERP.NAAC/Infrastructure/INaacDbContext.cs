using ERP.NAAC.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Infrastructure;

public interface INaacDbContext
{
    DbSet<SsrReport> SsrReports { get; }
    DbSet<SsrSection> SsrSections { get; }
    DbSet<DvvQuery> DvvQueries { get; }
    DbSet<AqarReport> AqarReports { get; }
    DbSet<AqarSection> AqarSections { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
