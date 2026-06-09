using ERP.ABC.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Infrastructure;

public interface IAbcDbContext
{
    DbSet<StudentAbcProfile> StudentAbcProfiles { get; }
    DbSet<CreditTransfer> CreditTransfers { get; }
    DbSet<AcademicPathway> AcademicPathways { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
