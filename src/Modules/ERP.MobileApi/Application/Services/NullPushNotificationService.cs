using ERP.MobileApi.Domain;
using ERP.MobileApi.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERP.MobileApi.Application.Services;

// Production: replace delivery logic only by implementing IPushNotificationService
// using Azure.Messaging.NotificationHubs SDK. This stub persists records so the
// in-app notification inbox works correctly during development.
public sealed class NullPushNotificationService : IPushNotificationService
{
    private readonly ILogger<NullPushNotificationService> _logger;
    private readonly IMobileDbContext _db;

    public NullPushNotificationService(ILogger<NullPushNotificationService> logger, IMobileDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<bool> SendToUserAsync(
        Guid tenantId,
        Guid userId,
        string title,
        string body,
        IDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[PUSH-NULL] To:{UserId} Title:{Title}", userId, title);

        var notification = new PushNotification
        {
            TenantId = tenantId,
            RecipientUserId = userId,
            Title = title,
            Body = body,
            Data = data != null ? System.Text.Json.JsonSerializer.Serialize(data) : null,
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow,
            Platform = DevicePlatform.Android,
            NotificationType = "General"
        };

        await _db.PushNotifications.AddAsync(notification, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> SendToUsersAsync(
        Guid tenantId,
        IReadOnlyList<Guid> userIds,
        string title,
        string body,
        IDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var uid in userIds)
            await SendToUserAsync(tenantId, uid, title, body, data, cancellationToken);
        return userIds.Count;
    }
}
