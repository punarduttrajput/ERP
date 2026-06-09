using ERP.Shared.Domain;

namespace ERP.Transport.Domain;

public class Route : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ReturnTime { get; set; }
    public bool IsActive { get; set; } = true;
    public int TotalStops { get; set; } = 0;
    public int TotalPassengers { get; set; } = 0;
    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
}
