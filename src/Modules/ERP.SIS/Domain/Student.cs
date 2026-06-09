using ERP.Shared.Domain;

namespace ERP.SIS.Domain;

public class Student : TenantEntity
{
    public string StudentNumber { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public DateTime EnrolledAt { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? BloodGroup { get; set; }

    public string? PermanentAddress { get; set; }
    public string? CurrentAddress { get; set; }

    public string Category { get; set; } = string.Empty;
    public int Semester { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();
    public ICollection<StudentFamily> FamilyDetails { get; set; } = new List<StudentFamily>();
}
