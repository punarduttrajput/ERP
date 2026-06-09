using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class Employee : TenantEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PanNumber { get; set; }
    public string? AadharNumber { get; set; }
    public DateOnly JoiningDate { get; set; }
    public DateOnly? ConfirmationDate { get; set; }
    public Guid? ReportingManagerId { get; set; }

    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
}
