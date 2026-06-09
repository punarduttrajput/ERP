using ERP.Shared.Domain;

namespace ERP.Timetable.Domain;

public class TimeSlot : TenantEntity
{
    public int DayOfWeek { get; set; }
    public int PeriodNumber { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsBreak { get; set; } = false;
}
