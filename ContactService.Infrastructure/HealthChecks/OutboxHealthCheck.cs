using ContactService.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ContactService.Infrastructure.HealthChecks;

public class OutboxHealthCheck : IHealthCheck
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<OutboxHealthCheck> _logger;
    private const int MaxPendingEventsThreshold = 100;
    private const int MaxFailedEventsThreshold = 10;

    public OutboxHealthCheck(IOutboxService outboxService, ILogger<OutboxHealthCheck> logger)
    {
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _outboxService.GetStatisticsAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["pendingEvents"] = statistics.PendingEvents,
                ["failedEvents"] = statistics.FailedEvents,
                ["lastProcessedAt"] = statistics.LastProcessedAt,
                ["maxPendingThreshold"] = MaxPendingEventsThreshold,
                ["maxFailedThreshold"] = MaxFailedEventsThreshold
            };

            // Check for too many failed events
            if (statistics.FailedEvents > MaxFailedEventsThreshold)
            {
                return HealthCheckResult.Unhealthy(
                    $"Too many failed outbox events: {statistics.FailedEvents} (threshold: {MaxFailedEventsThreshold})",
                    data: data);
            }

            // Check for too many pending events
            if (statistics.PendingEvents > MaxPendingEventsThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"High number of pending outbox events: {statistics.PendingEvents} (threshold: {MaxPendingEventsThreshold})",
                    data: data);
            }

            // Check if events are being processed recently (within last 10 minutes)
            var timeSinceLastProcessing = DateTime.UtcNow - statistics.LastProcessedAt;
            if (statistics.PendingEvents > 0 && timeSinceLastProcessing > TimeSpan.FromMinutes(10))
            {
                return HealthCheckResult.Degraded(
                    $"Outbox events haven't been processed for {timeSinceLastProcessing.TotalMinutes:F1} minutes",
                    data: data);
            }

            var status = statistics.PendingEvents > 50 ? "Busy processing events" : "Processing normally";
            return HealthCheckResult.Healthy($"Outbox is healthy - {status}", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outbox health check failed");
            return HealthCheckResult.Unhealthy("Failed to check outbox status", ex);
        }
    }
}