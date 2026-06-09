using ERP.Shared.Domain;

namespace ERP.Transport.Domain;

public class Driver : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public DateOnly LicenseExpiryDate { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
