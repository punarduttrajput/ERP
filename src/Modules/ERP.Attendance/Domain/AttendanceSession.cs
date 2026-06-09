using ERP.Shared.Domain;

namespace ERP.Attendance.Domain;

public class AttendanceSession : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Guid BatchId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid FacultyUserId { get; set; }
    public DateOnly SessionDate { get; set; }
    public int PeriodNumber { get; set; }
    public bool IsLocked { get; set; } = false;
    public string? QrToken { get; set; }
    public DateTime? QrExpiresAt { get; set; }

    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
}
