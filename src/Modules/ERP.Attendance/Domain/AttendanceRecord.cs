using ERP.Shared.Domain;

namespace ERP.Attendance.Domain;

public class AttendanceRecord : TenantEntity
{
    public Guid SessionId { get; set; }
    public Guid StudentId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime MarkedAt { get; set; }
    public string MarkedBy { get; set; } = string.Empty;

    public AttendanceSession? Session { get; set; }
}
