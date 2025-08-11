using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Text;

namespace ContactService.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisSettings> settings,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _database = connectionMultiplexer.GetDatabase(settings.Value.Database);
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return default(T);

        try
        {
            var cacheKey = GetCacheKey(key);
            var cachedValue = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
                return default(T);
            }

            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return JsonConvert.DeserializeObject<T>(cachedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache value for key: {Key}", key);
            return default(T);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return;

        try
        {
            var cacheKey = GetCacheKey(key);
            var serializedValue = JsonConvert.SerializeObject(value);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(_settings.DefaultExpiration);
            }

            await _distributedCache.SetStringAsync(cacheKey, serializedValue, options, cancellationToken);
            _logger.LogDebug("Cached value for key: {CacheKey} with expiration: {Expiration}", 
                cacheKey, expiration ?? _settings.DefaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return;

        try
        {
            var cacheKey = GetCacheKey(key);
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogDebug("Removed cache key: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return;

        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
            var cachePattern = GetCacheKey(pattern);
            
            await foreach (var key in server.KeysAsync(_settings.Database, cachePattern))
            {
                await _database.KeyDeleteAsync(key);
            }

            _logger.LogDebug("Removed cache keys matching pattern: {Pattern}", cachePattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return false;

        try
        {
            var cacheKey = GetCacheKey(key);
            return await _database.KeyExistsAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return;

        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
            await server.FlushDatabaseAsync(_settings.Database);
            _logger.LogInformation("Cleared all cache entries for database: {Database}", _settings.Database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || !items.Any())
            return;

        try
        {
            var tasks = items.Select(async kvp =>
            {
                await SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken);
            });

            await Task.WhenAll(tasks);
            _logger.LogDebug("Bulk cached {Count} items", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk cache set operation");
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, T?>();

        if (!_settings.Enabled)
            return result;

        try
        {
            var tasks = keys.Select(async key =>
            {
                var value = await GetAsync<T>(key, cancellationToken);
                return new KeyValuePair<string, T?>(key, value);
            });

            var results = await Task.WhenAll(tasks);
            
            foreach (var kvp in results)
            {
                result[kvp.Key] = kvp.Value;
            }

            _logger.LogDebug("Bulk retrieved {Count} cache items", keys.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk cache get operation");
        }

        return result;
    }

    private string GetCacheKey(string key)
    {
        return $"{_settings.InstanceName}:{key}";
    }
}