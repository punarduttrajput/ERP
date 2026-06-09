using ERP.Shared.Domain;

namespace ERP.Auth.Domain;

public class RefreshToken : TenantEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty; // for family revocation
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public bool IsUsed { get; set; } = false;
    public string? ReplacedByToken { get; set; }
    public string? RevokedReason { get; set; }
    public string? CreatedByIp { get; set; }

    public User? User { get; set; }

    public bool IsActive => !IsRevoked && !IsUsed && DateTime.UtcNow < ExpiresAt;
}
