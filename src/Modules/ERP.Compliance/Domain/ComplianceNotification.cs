using ERP.Shared.Domain;

namespace ERP.Compliance.Domain;

public class ComplianceNotification : TenantEntity
{
    public Guid ComplianceItemId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public Guid RecipientUserId { get; set; }
    public bool IsRead { get; set; } = false;
    public string NotificationType { get; set; } = string.Empty;
}
