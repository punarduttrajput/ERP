using ERP.Shared.Domain;

namespace ERP.Reporting.Domain;

public class ReportSchedule : TenantEntity
{
    public Guid ReportId { get; set; }
    public ScheduleFrequency Frequency { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public int RunAtHour { get; set; } = 7;
    public ExportFormat ExportFormat { get; set; }
    public string Recipients { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
}
