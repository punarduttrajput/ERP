using ERP.Hostel.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Infrastructure;

public interface IHostelDbContext
{
    DbSet<HostelBlock> HostelBlocks { get; }
    DbSet<HostelRoom> HostelRooms { get; }
    DbSet<RoomAllocation> RoomAllocations { get; }
    DbSet<WaitlistEntry> HostelWaitlist { get; }
    DbSet<VisitorEntry> VisitorEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
