using ERP.Shared.Domain;

namespace ERP.Tenants.Domain;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;  // used for subdomain
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? CustomDomain { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Plan { get; set; } = "standard";
    public DateTime? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }

    public TenantSettings? Settings { get; set; }
}

public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Inactive = 3
}
