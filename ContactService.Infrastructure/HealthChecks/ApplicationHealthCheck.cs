using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace ContactService.Infrastructure.HealthChecks;

public class ApplicationHealthCheck : IHealthCheck
{
    private readonly ILogger<ApplicationHealthCheck> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public ApplicationHealthCheck(ILogger<ApplicationHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - _startTime;
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";

            // Get memory usage
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            
            // Convert to MB for readability
            var workingSetMB = workingSet / (1024 * 1024);
            var privateMemoryMB = privateMemory / (1024 * 1024);

            var data = new Dictionary<string, object>
            {
                ["version"] = version,
                ["uptime"] = uptime.ToString(@"dd\.hh\:mm\:ss"),
                ["uptimeSeconds"] = uptime.TotalSeconds,
                ["workingSetMB"] = workingSetMB,
                ["privateMemoryMB"] = privateMemoryMB,
                ["threadCount"] = process.Threads.Count,
                ["processId"] = process.Id,
                ["machineName"] = Environment.MachineName,
                ["osVersion"] = Environment.OSVersion.ToString(),
                ["frameworkVersion"] = Environment.Version.ToString()
            };

            // Check memory usage thresholds
            if (workingSetMB > 1024) // 1GB threshold
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High memory usage: {workingSetMB}MB working set",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"Application is healthy (uptime: {uptime:dd\\.hh\\:mm\\:ss})", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check application health", ex));
        }
    }
}