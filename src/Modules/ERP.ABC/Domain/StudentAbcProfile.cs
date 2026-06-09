using ERP.Shared.Domain;

namespace ERP.ABC.Domain;

public class StudentAbcProfile : TenantEntity
{
    public Guid StudentId { get; set; }
    public string AbcId { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
    public string? RegistryStudentName { get; set; }
    public int TotalCreditsEarned { get; set; } = 0;
    public int TotalCreditsTransferredIn { get; set; } = 0;
    public int TotalCreditsTransferredOut { get; set; } = 0;
    public PathwayType? ActivePathwayType { get; set; }
}
