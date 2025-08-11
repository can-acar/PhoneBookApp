using ContactService.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ContactService.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisSettings> settings,
        ILogger<RedisHealthCheck> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.Enabled)
            {
                return HealthCheckResult.Healthy("Redis caching is disabled", new Dictionary<string, object>
                {
                    ["enabled"] = false
                });
            }

            var database = _connectionMultiplexer.GetDatabase(_settings.Database);

            // Test basic connectivity
            var testKey = $"{_settings.InstanceName}:healthcheck:test";
            var testValue = DateTime.UtcNow.ToString();
            
            // Set and get a test value
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(30));
            var retrievedValue = await database.StringGetAsync(testKey);
            
            if (retrievedValue != testValue)
            {
                return HealthCheckResult.Unhealthy("Redis set/get operation failed");
            }

            // Clean up test key
            await database.KeyDeleteAsync(testKey);

            var data = new Dictionary<string, object>
            {
                ["enabled"] = _settings.Enabled,
                ["database"] = _settings.Database,
                ["instanceName"] = _settings.InstanceName,
                ["connectionString"] = _settings.ConnectionString,
                ["isConnected"] = _connectionMultiplexer.IsConnected,
                ["endpoints"] = _connectionMultiplexer.GetEndPoints().Select(ep => ep.ToString()).ToArray(),
                ["testResult"] = "Success"
            };

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection failed");
            return HealthCheckResult.Degraded("Redis connection failed", ex, new Dictionary<string, object>
            {
                ["enabled"] = _settings.Enabled,
                ["connectionString"] = _settings.ConnectionString,
                ["error"] = "Connection failed"
            });
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogError(ex, "Redis operation timed out");
            return HealthCheckResult.Degraded("Redis operations are slow", ex, new Dictionary<string, object>
            {
                ["enabled"] = _settings.Enabled,
                ["connectionString"] = _settings.ConnectionString,
                ["error"] = "Timeout"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Degraded("Redis health check failed", ex, new Dictionary<string, object>
            {
                ["enabled"] = _settings.Enabled,
                ["error"] = ex.Message
            });
        }
    }
}