using System.Text.Json;
using ERP.Shared.Application.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ERP.Shared.Infrastructure;

/// <summary>
/// Redis-backed cache service with graceful degradation.
/// When Redis is unavailable all operations are no-ops / return null so the
/// application continues to function — just without the cache layer.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis  = redis;
        _db     = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>(value!, JsonOptions);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable — cache miss for key '{Key}'", key);
            return null;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error on GET key '{Key}'", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, JsonOptions);
            await _db.StringSetAsync(key, serialized, expiry);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable — skipping cache SET for key '{Key}'", key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error on SET key '{Key}'", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable — skipping cache DELETE for key '{Key}'", key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error on DELETE key '{Key}'", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys   = server.Keys(pattern: pattern).ToArray();
                if (keys.Length > 0)
                    await _db.KeyDeleteAsync(keys);
            }
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable — skipping pattern DELETE for '{Pattern}'", pattern);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error on pattern DELETE '{Pattern}'", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable — EXISTS check returning false for key '{Key}'", key);
            return false;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error on EXISTS key '{Key}'", key);
            return false;
        }
    }
}
