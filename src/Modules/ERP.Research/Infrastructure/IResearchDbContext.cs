using ERP.Research.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Infrastructure;

public interface IResearchDbContext
{
    DbSet<ResearchProject> ResearchProjects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<Publication> Publications { get; }
    DbSet<Patent> Patents { get; }
    DbSet<Grant> Grants { get; }
    DbSet<GrantDisbursement> GrantDisbursements { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
