using ERP.Shared.Domain;

namespace ERP.Attendance.Domain;

public class BiometricLog : TenantEntity
{
    public string DeviceId { get; set; } = string.Empty;
    public string BiometricId { get; set; } = string.Empty;
    public Guid? StudentId { get; set; }
    public DateTime LoggedAt { get; set; }
    public bool IsProcessed { get; set; } = false;
}
