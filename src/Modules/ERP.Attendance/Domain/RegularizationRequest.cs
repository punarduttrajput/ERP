using ERP.Shared.Domain;

namespace ERP.Attendance.Domain;

public class RegularizationRequest : TenantEntity
{
    public Guid SessionId { get; set; }
    public Guid StudentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RegularizationStatus Status { get; set; } = RegularizationStatus.Pending;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewRemark { get; set; }
}
