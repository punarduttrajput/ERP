using ERP.Transport.Application.Commands;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ERP.Transport.API;

[ApiController]
[Route("api/transport/gps/webhook")]
public class GpsWebhookController : ControllerBase
{
    private readonly IGpsProvider _gpsProvider;
    private readonly ITransportDbContext _db;
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;

    public GpsWebhookController(
        IGpsProvider gpsProvider,
        ITransportDbContext db,
        IMediator mediator,
        IConfiguration config)
    {
        _gpsProvider = gpsProvider;
        _db = db;
        _mediator = mediator;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        string payload;
        using (var reader = new System.IO.StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["X-Gps-Signature"].FirstOrDefault() ?? string.Empty;
        var secret = _config["Transport:GpsWebhookSecret"] ?? string.Empty;

        if (!_gpsProvider.ValidateWebhook(payload, signature, secret))
            return Ok(); // Return 200 to prevent GPS provider retry storms on auth failures

        var update = _gpsProvider.ParseWebhook(payload);
        if (update is null)
            return Ok();

        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.RegistrationNumber == update.VehicleRegistration && !v.IsDeleted, ct);
        if (vehicle is null)
            return Ok(); // Silently ignore unknown registrations to prevent GPS provider retries

        var cmd = new UpdateGpsLocationCommand(
            vehicle.Id,
            update.Latitude,
            update.Longitude,
            update.Speed,
            update.Heading,
            update.RecordedAt,
            update.ProviderReference);

        await _mediator.Send(cmd, ct);

        return Ok();
    }
}
