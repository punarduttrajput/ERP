using ERP.Shared.Domain;

namespace ERP.MobileApi.Domain;

public class PushNotification : TenantEntity
{
    public Guid RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Data { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? FailureReason { get; set; }
    public DevicePlatform Platform { get; set; }
    public string NotificationType { get; set; } = string.Empty;
}
