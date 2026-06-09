using ERP.Shared.Domain;

namespace ERP.Accreditation.Domain;

public class EvidenceTag : TenantEntity
{
    public string ModuleName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string RecordLabel { get; set; } = string.Empty;
    public string NaacCriterion { get; set; } = string.Empty;
    public string NaacIndicator { get; set; } = string.Empty;
    public Guid TaggedBy { get; set; }
    public string? Notes { get; set; }
}
