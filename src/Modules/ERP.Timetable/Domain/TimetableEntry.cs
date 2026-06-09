using ERP.Shared.Domain;

namespace ERP.Timetable.Domain;

public class TimetableEntry : TenantEntity
{
    public Guid BatchId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid FacultyUserId { get; set; }
    public Guid RoomId { get; set; }
    public Guid TimeSlotId { get; set; }
    public TimetableStatus Status { get; set; }
    public int? Week { get; set; }
    public bool IsSubstitute { get; set; } = false;
    public Guid? SubstituteForEntryId { get; set; }

    public Room? Room { get; set; }
    public TimeSlot? TimeSlot { get; set; }
}
