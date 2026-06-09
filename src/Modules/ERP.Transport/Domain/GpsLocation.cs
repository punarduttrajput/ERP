using ERP.Shared.Domain;

namespace ERP.Transport.Domain;

public class GpsLocation : TenantEntity
{
    public Guid VehicleId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Heading { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? ProviderReference { get; set; }
}
