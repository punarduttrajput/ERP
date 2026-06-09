using ERP.Shared.Domain;

namespace ERP.Hostel.Domain;

public class HostelBlock : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int TotalRooms { get; set; } = 0;
    public int OccupiedRooms { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<HostelRoom> Rooms { get; set; } = new List<HostelRoom>();
}
