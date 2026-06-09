using ERP.Compliance.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Compliance.Infrastructure;

public interface IComplianceDbContext
{
    DbSet<ComplianceItem> ComplianceItems { get; }
    DbSet<AisheReturn> AisheReturns { get; }
    DbSet<ComplianceNotification> ComplianceNotifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
