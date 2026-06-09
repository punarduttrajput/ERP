using ERP.Shared.Domain;

namespace ERP.Hostel.Domain;

public class WaitlistEntry : TenantEntity
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public RoomType PreferredRoomType { get; set; }
    public Guid? PreferredBlockId { get; set; }
    public int AcademicYear { get; set; }
    public DateTime RequestedAt { get; set; }
    public int Priority { get; set; }
    public bool IsPromoted { get; set; } = false;
}
