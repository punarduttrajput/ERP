using ERP.Shared.Domain;

namespace ERP.NAAC.Domain;

public class SsrSection : TenantEntity
{
    public Guid SsrId { get; set; }
    public string CriterionNumber { get; set; } = string.Empty;
    public string IndicatorNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AutoMetrics { get; set; }
    public Guid? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }

    public SsrReport Ssr { get; set; } = null!;
}
