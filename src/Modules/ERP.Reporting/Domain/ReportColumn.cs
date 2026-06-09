using ERP.Shared.Domain;

namespace ERP.Reporting.Domain;

public class ReportColumn : TenantEntity
{
    public Guid ReportId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public int OrderIndex { get; set; }
    public string? Format { get; set; }
    public ReportDefinition? Report { get; set; }
}
