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
            var resolved = await TryResolveFromRedisAsync(slug, redis, currentTenant);
            if (!resolved)
            {
                await TryResolveFromDbAsync(slug, db, redis, currentTenant);
            }

            if (!currentTenant.IsResolved)
                _logger.LogWarning("Tenant slug '{Slug}' could not be resolved from Redis or DB.", slug);
        }

        await _next(context);
    }

    private static string? ExtractSlug(HttpContext context)
    {
        // Try X-Tenant-Slug header first
        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var headerSlug))
            return headerSlug.ToString().ToLowerInvariant();

        // Try subdomain: tenant.example.com
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length >= 3)
            return parts[0].ToLowerInvariant();

        return null;
    }

    private async Task<bool> TryResolveFromRedisAsync(string slug, IConnectionMultiplexer redis, ICurrentTenant currentTenant)
    {
        try
        {
            var db = redis.GetDatabase();
            var cacheKey = $"tenant:slug:{slug}";
            var cached = await db.StringGetAsync(cacheKey);

            if (!cached.HasValue)
                return false;

            var tenantInfo = JsonSerializer.Deserialize<CachedTenantInfo>(cached!);
            if (tenantInfo is null)
                return false;

            currentTenant.SetTenant(tenantInfo.TenantId, tenantInfo.Slug);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis error during tenant resolution for slug: {Slug}", slug);
            return false;
        }
    }

    private async Task TryResolveFromDbAsync(string slug, AppDbContext db, IConnectionMultiplexer redis, ICurrentTenant currentTenant)
    {
        try
        {
            var tenant = await db.Set<ERP.Tenants.Domain.Tenant>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted
                    && t.Status == ERP.Tenants.Domain.TenantStatus.Active);

            if (tenant is null)
            {
                _logger.LogWarning("Tenant not found for slug: {Slug}", slug);
                return;
            }

            currentTenant.SetTenant(tenant.Id, tenant.Slug);

            // Cache for 5 minutes
            var redisDb = redis.GetDatabase();
            var cacheKey = $"tenant:slug:{slug}";
            var info = new CachedTenantInfo(tenant.Id, tenant.Slug);
            await redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(info), TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error during tenant resolution for slug: {Slug}", slug);
        }
    }

    private record CachedTenantInfo(Guid TenantId, string Slug);
}
