using ERP.Shared.Domain;

namespace ERP.NAAC.Domain;

public class AqarSection : TenantEntity
{
    public Guid AqarId { get; set; }
    public string CriterionNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid? AssignedTo { get; set; }
    public string? Content { get; set; }
    public AqarStatus Status { get; set; } = AqarStatus.Draft;
    public string? ReviewComment { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public AqarReport Aqar { get; set; } = null!;
}
