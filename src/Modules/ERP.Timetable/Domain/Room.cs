using ERP.Shared.Domain;

namespace ERP.Timetable.Domain;

public class Room : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public string? Building { get; set; }
    public int? Floor { get; set; }
    public bool IsActive { get; set; } = true;
}
