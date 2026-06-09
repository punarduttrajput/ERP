using ERP.Analytics.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Analytics.Infrastructure;

public interface IAnalyticsDbContext
{
    DbSet<StudentRiskScore> StudentRiskScores { get; }
    DbSet<FeeDefaultRiskScore> FeeDefaultRiskScores { get; }
    DbSet<PlacementScore> PlacementScores { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
