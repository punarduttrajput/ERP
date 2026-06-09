using ERP.Shared.Domain;

namespace ERP.Tenants.Domain;

public class TenantSettings : BaseEntity
{
    public Guid TenantId { get; set; }
    public string? TimeZone { get; set; } = "UTC";
    public string? DateFormat { get; set; } = "yyyy-MM-dd";
    public string? CurrencyCode { get; set; } = "USD";
    public string? Language { get; set; } = "en";
    public int MaxUsersAllowed { get; set; } = 500;
    public bool EnableNotifications { get; set; } = true;
    public bool EnableTwoFactorAuth { get; set; } = false;
    public string? CustomCss { get; set; }

    public Tenant? Tenant { get; set; }
}
