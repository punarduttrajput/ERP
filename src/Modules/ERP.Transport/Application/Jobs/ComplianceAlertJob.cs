using ERP.Shared.Application.Abstractions;
using ERP.Transport.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ERP.Transport.Application.Jobs;

public class ComplianceAlertJob
{
    private readonly ITransportDbContext _db;
    private readonly ISmsService _sms;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _config;
    private readonly ILogger<ComplianceAlertJob> _logger;

    public ComplianceAlertJob(
        ITransportDbContext db,
        ISmsService sms,
        IConnectionMultiplexer redis,
        IConfiguration config,
        ILogger<ComplianceAlertJob> logger)
    {
        _db = db;
        _sms = sms;
        _redis = redis;
        _config = config;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(30);
        var cache = _redis.GetDatabase();
        var managerMobile = _config["Transport:ManagerMobile"] ?? string.Empty;
        var dateStr = today.ToString("yyyy-MM-dd");

        // IgnoreQueryFilters() so the job scans all tenants — required because Hangfire runs outside
        // an HTTP request context where ICurrentTenant.TenantId would be null, and per-tenant query
        // filters would silently return empty sets, causing compliance alerts to never fire.
        var vehicles = await _db.Vehicles
            .IgnoreQueryFilters()
            .Where(v => v.IsActive && !v.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var v in vehicles)
        {
            await CheckAndAlertVehicle(cache, v.TenantId, v.Id, v.RegistrationNumber,
                "fitness", v.FitnessExpiryDate, threshold, today, dateStr, managerMobile, cancellationToken);
            await CheckAndAlertVehicle(cache, v.TenantId, v.Id, v.RegistrationNumber,
                "insurance", v.InsuranceExpiryDate, threshold, today, dateStr, managerMobile, cancellationToken);
            await CheckAndAlertVehicle(cache, v.TenantId, v.Id, v.RegistrationNumber,
                "pollution", v.PollutionExpiryDate, threshold, today, dateStr, managerMobile, cancellationToken);
        }

        var drivers = await _db.Drivers
            .IgnoreQueryFilters()
            .Where(d => d.IsActive && !d.IsDeleted && d.LicenseExpiryDate <= threshold)
            .ToListAsync(cancellationToken);

        foreach (var d in drivers)
        {
            var key = $"transport_alert:{d.TenantId}:{d.Id}:license:{dateStr}";
            if (await cache.KeyExistsAsync(key))
                continue;

            var msg = $"ALERT: Driver {d.Name} (License: {d.LicenseNumber}) license expires on {d.LicenseExpiryDate:dd-MMM-yyyy}. Please renew.";
            try
            {
                await _sms.SendAsync(d.MobileNumber, msg, cancellationToken);
                await cache.StringSetAsync(key, "1", TimeSpan.FromHours(25));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send license expiry SMS for driver {DriverId}", d.Id);
            }
        }
    }

    private async Task CheckAndAlertVehicle(
        IDatabase cache,
        Guid tenantId,
        Guid vehicleId,
        string regNumber,
        string alertType,
        DateOnly expiryDate,
        DateOnly threshold,
        DateOnly today,
        string dateStr,
        string managerMobile,
        CancellationToken cancellationToken)
    {
        if (expiryDate > threshold)
            return;

        var key = $"transport_alert:{tenantId}:{vehicleId}:{alertType}:{dateStr}";
        if (await cache.KeyExistsAsync(key))
            return;

        var docName = alertType switch
        {
            "fitness" => "fitness certificate",
            "insurance" => "insurance",
            "pollution" => "pollution certificate",
            _ => alertType
        };

        var msg = $"ALERT: {regNumber} {docName} expires on {expiryDate:dd-MMM-yyyy}. Please renew.";
        try
        {
            await _sms.SendAsync(managerMobile, msg, cancellationToken);
            await cache.StringSetAsync(key, "1", TimeSpan.FromHours(25));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {AlertType} expiry SMS for vehicle {VehicleId}", alertType, vehicleId);
        }
    }
}
