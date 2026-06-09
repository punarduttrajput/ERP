using ERP.Alumni.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Infrastructure;

public interface IAlumniDbContext
{
    DbSet<AlumniProfile> AlumniProfiles { get; }
    DbSet<AlumniEvent> AlumniEvents { get; }
    DbSet<EventRegistration> EventRegistrations { get; }
    DbSet<DonationCampaign> DonationCampaigns { get; }
    DbSet<DonationPledge> DonationPledges { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
