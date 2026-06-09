using ERP.Shared.Domain;

namespace ERP.MobileApi.Domain;

public class DeviceRegistration : TenantEntity
{
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public string? AppVersion { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
