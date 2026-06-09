using ERP.Shared.Domain;

namespace ERP.Hostel.Domain;

public class VisitorEntry : TenantEntity
{
    public string VisitorName { get; set; } = string.Empty;
    public string VisitorMobile { get; set; } = string.Empty;
    public string VisitorIdType { get; set; } = string.Empty;
    public string VisitorIdNumber { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid BlockId { get; set; }
    public string PurposeOfVisit { get; set; } = string.Empty;
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public Guid CheckedInBy { get; set; }
}
