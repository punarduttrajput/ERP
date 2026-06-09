namespace ERP.Transport.Domain;

public interface IGpsProvider
{
    bool ValidateWebhook(string payload, string signature, string secret);
    GpsLocationUpdate? ParseWebhook(string payload);
}

public record GpsLocationUpdate(
    string VehicleRegistration,
    decimal Latitude,
    decimal Longitude,
    decimal? Speed,
    decimal? Heading,
    DateTime RecordedAt,
    string? ProviderReference);
