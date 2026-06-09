using ERP.Shared.Domain;

namespace ERP.Reporting.Domain;

public class ReportFilter : TenantEntity
{
    public Guid ReportId { get; set; }
    public string FilterKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FilterType { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public string? DefaultValue { get; set; }
    public string? Options { get; set; }
    public ReportDefinition? Report { get; set; }
}
