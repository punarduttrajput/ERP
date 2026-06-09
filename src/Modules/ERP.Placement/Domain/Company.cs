using ERP.Shared.Domain;

namespace ERP.Placement.Domain;

public class Company : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactMobile { get; set; }
    public int TotalDrives { get; set; } = 0;
    public int TotalOffers { get; set; } = 0;
    public decimal HighestPackageLpa { get; set; } = 0;
    public decimal AveragePackageLpa { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<PlacementDrive> Drives { get; set; } = new List<PlacementDrive>();
}
