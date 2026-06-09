using ERP.SIS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Infrastructure;

public interface ISisDbContext
{
    DbSet<Student> Students { get; }
    DbSet<StudentDocument> StudentDocuments { get; }
    DbSet<StudentFamily> StudentFamilyDetails { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
