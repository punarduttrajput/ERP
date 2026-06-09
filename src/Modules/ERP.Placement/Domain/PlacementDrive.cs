using ERP.Shared.Domain;

namespace ERP.Placement.Domain;

public class PlacementDrive : TenantEntity
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobRole { get; set; } = string.Empty;
    public string? JobDescription { get; set; }
    public string? Location { get; set; }
    public decimal PackageLpa { get; set; }
    public decimal MinCgpa { get; set; } = 0;
    public int MaxBacklogs { get; set; } = 0;
    public string? EligibleBranches { get; set; }
    public DateOnly? DriveDate { get; set; }
    public DateOnly? RegistrationDeadline { get; set; }
    public DriveStatus Status { get; set; }
    public int AcademicYear { get; set; }
    public int TotalRegistrations { get; set; } = 0;
    public int TotalSelected { get; set; } = 0;

    public Company? Company { get; set; }
    public ICollection<DriveRegistration> Registrations { get; set; } = new List<DriveRegistration>();
}
