using ERP.NIRF.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.NIRF.Infrastructure;

public interface INirfDbContext
{
    DbSet<NirfSubmission> NirfSubmissions { get; }
    DbSet<NirfParameterScore> NirfParameterScores { get; }
    DbSet<NirfRankEntry> NirfRankHistory { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
