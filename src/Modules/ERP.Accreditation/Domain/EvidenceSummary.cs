using ERP.Shared.Domain;

namespace ERP.Accreditation.Domain;

public class EvidenceSummary : TenantEntity
{
    public int AcademicYear { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MetricKey { get; set; } = string.Empty;
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public DateTime ComputedAt { get; set; }
}
