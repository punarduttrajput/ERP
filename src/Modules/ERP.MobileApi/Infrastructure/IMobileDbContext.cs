using ERP.MobileApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.Infrastructure;

public interface IMobileDbContext
{
    DbSet<DeviceRegistration> DeviceRegistrations { get; }
    DbSet<PushNotification> PushNotifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
