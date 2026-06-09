using ERP.Shared.Domain;

namespace ERP.Hostel.Domain;

public class HostelRoom : TenantEntity
{
    public Guid BlockId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public RoomType RoomType { get; set; }
    public int Capacity { get; set; }
    public int OccupiedCount { get; set; } = 0;
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public decimal MonthlyRent { get; set; }
    public bool IsActive { get; set; } = true;

    public HostelBlock? Block { get; set; }
    public ICollection<RoomAllocation> Allocations { get; set; } = new List<RoomAllocation>();
}
