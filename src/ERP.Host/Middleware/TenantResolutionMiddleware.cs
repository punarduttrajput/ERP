using ERP.Shared.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace ERP.Host.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant,
        IConnectionMultiplexer redis, AppDbContext db)
    {
        var slug = ExtractSlug(context);

        if (!string.IsNullOrWhiteSpace(slug))
        {
            // Resolve tenant info from Redis or DB — returns null if not found.
            // SetTenant MUST be called here in InvokeAsync (parent context) so the
            // AsyncLocal value flows forward to _next(context). AsyncLocal changes
            // made inside awaited child methods do NOT propagate back to the parent.
            var tenantInfo = await ResolveAsync(slug, redis, db);

            if (tenantInfo.HasValue)
            {
                currentTenant.SetTenant(tenantInfo.Value.TenantId, tenantInfo.Value.Slug);
                _logger.LogDebug("Tenant resolved: {Slug} → {TenantId}", slug, tenantInfo.Value.TenantId);
            }
            else
            {
                _logger.LogWarning("Tenant not found for slug: {Slug}", slug);
            }
        }

        await _next(context);
    }

    private static string? ExtractSlug(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var headerSlug))
            return headerSlug.ToString().ToLowerInvariant();

        var host  = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length >= 3)
            return parts[0].ToLowerInvariant();

        return null;
    }

    private async Task<(Guid TenantId, string Slug)?> ResolveAsync(
        string slug, IConnectionMultiplexer redis, AppDbContext db)
    {
        // Try Redis first
        var fromRedis = await TryRedisAsync(slug, redis);
        if (fromRedis.HasValue) return fromRedis;

        // Fall back to DB
        return await TryDbAsync(slug, db);
    }

    private async Task<(Guid TenantId, string Slug)?> TryRedisAsync(
        string slug, IConnectionMultiplexer redis)
    {
        try
        {
            var redisDb  = redis.GetDatabase();
            var cacheKey = $"tenant:slug:{slug}";
            var cached   = await redisDb.StringGetAsync(cacheKey);

            if (!cached.HasValue) return null;

            var info = JsonSerializer.Deserialize<CachedTenantInfo>(cached!);
            if (info is null) return null;

            return (info.TenantId, info.Slug);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis error during tenant resolution for slug: {Slug}", slug);
            return null;
        }
    }

    private async Task<(Guid TenantId, string Slug)?> TryDbAsync(
        string slug, AppDbContext db)
    {
        try
        {
            var tenant = await db.Set<ERP.Tenants.Domain.Tenant>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted);

            if (tenant is null) return null;

            return (tenant.Id, tenant.Slug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error during tenant resolution for slug: {Slug}", slug);
            return null;
        }
    }

    private record CachedTenantInfo(Guid TenantId, string Slug);
}
