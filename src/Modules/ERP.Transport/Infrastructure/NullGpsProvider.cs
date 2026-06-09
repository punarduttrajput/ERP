using ERP.Transport.Domain;

namespace ERP.Transport.Infrastructure;

public sealed class NullGpsProvider : IGpsProvider
{
    public bool ValidateWebhook(string payload, string signature, string secret) => true;

    public GpsLocationUpdate? ParseWebhook(string payload)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(payload);
            var root = doc.RootElement;
            return new GpsLocationUpdate(
                root.GetProperty("reg").GetString()!,
                root.GetProperty("lat").GetDecimal(),
                root.GetProperty("lng").GetDecimal(),
                root.TryGetProperty("speed", out var s) ? s.GetDecimal() : null,
                root.TryGetProperty("heading", out var h) ? h.GetDecimal() : null,
                root.TryGetProperty("recordedAt", out var t) ? t.GetDateTime() : DateTime.UtcNow,
                null);
        }
        catch { return null; }
    }
}
