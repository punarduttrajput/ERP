using ERP.Shared.Domain;

namespace ERP.Alumni.Domain;

public class AlumniProfile : TenantEntity
{
    public Guid? StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public int GraduationYear { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string? BatchName { get; set; }
    public string? CurrentEmployer { get; set; }
    public string? CurrentJobTitle { get; set; }
    public string? CurrentCity { get; set; }
    public string CurrentCountry { get; set; } = "India";
    public string? LinkedInUrl { get; set; }
    public bool IsDirectoryVisible { get; set; } = true;
    public bool IsVerified { get; set; } = false;
    public string? AvatarUrl { get; set; }
}
