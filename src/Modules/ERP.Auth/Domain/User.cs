using ERP.Shared.Domain;

namespace ERP.Auth.Domain;

public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // MFA — TOTP
    public bool MfaEnabled { get; set; } = false;
    public string? MfaSecret { get; set; }
    public string? MfaRecoveryCodes { get; set; } // JSON array of SHA-256 hashed codes

    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsLockedOut => LockoutEndAt.HasValue && LockoutEndAt > DateTime.UtcNow;
}
