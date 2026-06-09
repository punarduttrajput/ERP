using ERP.Shared.Domain;

namespace ERP.Reporting.Domain;

public class ReportDefinition : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportCategory Category { get; set; }
    public string SqlQuery { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string? DefaultColumns { get; set; }
    public string? AvailableFilters { get; set; }
    public ICollection<ReportColumn> Columns { get; set; } = new List<ReportColumn>();
    public ICollection<ReportFilter> Filters { get; set; } = new List<ReportFilter>();
}
