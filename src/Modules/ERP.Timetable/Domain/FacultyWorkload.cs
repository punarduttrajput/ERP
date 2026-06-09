using ERP.Shared.Domain;

namespace ERP.Timetable.Domain;

public class FacultyWorkload : TenantEntity
{
    public Guid FacultyUserId { get; set; }
    public Guid SemesterId { get; set; }
    public int MaxHoursPerWeek { get; set; }
    public int AssignedHoursPerWeek { get; set; } = 0;
}
