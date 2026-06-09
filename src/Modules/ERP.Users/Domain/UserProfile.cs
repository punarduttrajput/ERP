using ERP.Shared.Domain;

namespace ERP.Users.Domain;

/// <summary>
/// Extended profile for a user.  Id == User.Id (1-to-1, shared primary key).
/// Auth module owns: email, password, lockout flags, IsActive.
/// This entity owns: display name, phone, avatar, department, job title.
/// </summary>
public class UserProfile : TenantEntity
{
    // Id is inherited from BaseEntity and equals User.Id
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
