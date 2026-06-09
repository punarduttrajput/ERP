using ERP.Admissions.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Admissions.Infrastructure;

public interface IAdmissionsDbContext
{
    DbSet<AdmissionApplication> Applications { get; }
    DbSet<ApplicationDocument> ApplicationDocuments { get; }
    DbSet<WorkflowAuditEntry> WorkflowAuditEntries { get; }
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<SeatMatrix> SeatMatrices { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
