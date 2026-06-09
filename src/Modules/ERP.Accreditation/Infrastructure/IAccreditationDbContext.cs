using ERP.Accreditation.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Accreditation.Infrastructure;

public interface IAccreditationDbContext
{
    DbSet<EvidenceTag> EvidenceTags { get; }
    DbSet<EvidenceSummary> EvidenceSummaries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
