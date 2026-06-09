using ERP.Shared.Domain;

namespace ERP.Reporting.Domain;

public class ReportExecution : TenantEntity
{
    public Guid ReportId { get; set; }
    public Guid? ExecutedBy { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? FiltersJson { get; set; }
    public int RowCount { get; set; }
    public long DurationMs { get; set; }
    public ExportFormat? ExportFormat { get; set; }
    public bool IsScheduled { get; set; } = false;
}
