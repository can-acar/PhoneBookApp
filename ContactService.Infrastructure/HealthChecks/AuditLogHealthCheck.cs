using ContactService.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ContactService.Infrastructure.HealthChecks;

/// <summary>
/// Health check for audit logging system
/// Verifies MongoDB audit log repository connectivity and performance
/// </summary>
public class AuditLogHealthCheck : IHealthCheck
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditLogHealthCheck> _logger;

    public AuditLogHealthCheck(IAuditLogRepository auditLogRepository, ILogger<AuditLogHealthCheck> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Test basic connectivity and performance
            var isHealthy = await _auditLogRepository.IsHealthyAsync(cancellationToken);
            
            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            if (!isHealthy)
            {
                _logger.LogWarning("Audit log repository health check failed - connectivity issue");
                return new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    "Audit log repository is not accessible",
                    data: new Dictionary<string, object>
                    {
                        ["responseTime"] = responseTime,
                        ["connectivity"] = false
                    });
            }

            // Get additional statistics for monitoring
            var totalCount = await _auditLogRepository.GetTotalCountAsync(cancellationToken);
            
            stopwatch.Stop();
            var totalResponseTime = stopwatch.ElapsedMilliseconds;

            // Determine health status based on response time
            var status = totalResponseTime < 500 ? HealthStatus.Healthy :
                        totalResponseTime < 2000 ? HealthStatus.Degraded :
                                                   HealthStatus.Unhealthy;

            var description = status switch
            {
                HealthStatus.Healthy => "Audit logging system is operating normally",
                HealthStatus.Degraded => "Audit logging system is responding slowly",
                HealthStatus.Unhealthy => "Audit logging system is experiencing significant delays",
                _ => "Audit logging system status unknown"
            };

            return new HealthCheckResult(
                status,
                description,
                data: new Dictionary<string, object>
                {
                    ["responseTime"] = totalResponseTime,
                    ["totalAuditLogs"] = totalCount,
                    ["connectivity"] = true,
                    ["threshold_healthy"] = 500,
                    ["threshold_degraded"] = 2000
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit log health check failed with exception");
            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                "Audit logging system health check failed",
                ex,
                new Dictionary<string, object>
                {
                    ["connectivity"] = false,
                    ["error"] = ex.Message
                });
        }
    }
}