using ERP.Shared.Domain;

namespace ERP.Transport.Domain;

public class RouteStop : TenantEntity
{
    public Guid RouteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public TimeOnly? PickupTime { get; set; }
    public decimal? DistanceFromCollegeKm { get; set; }
    public Route? Route { get; set; }
}
