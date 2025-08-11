using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.HealthChecks;

public class NotificationProvidersHealthCheck : IHealthCheck
{
    private readonly INotificationProviderManager _providerManager;
    private readonly ILogger<NotificationProvidersHealthCheck> _logger;

    public NotificationProvidersHealthCheck(
        INotificationProviderManager providerManager,
        ILogger<NotificationProvidersHealthCheck> logger)
    {
        _providerManager = providerManager;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var availableProviders = new List<ProviderType> { ProviderType.Email, ProviderType.Sms };
            var healthResults = new Dictionary<string, object>();
            var healthyProviders = 0;
            var totalProviders = availableProviders.Count;
            var degradedProviders = new List<string>();
            var failedProviders = new List<string>();

            foreach (var providerType in availableProviders)
            {
                try
                {
                    // Get provider and check if it's available
                    var provider = _providerManager.GetProvider(providerType);
                    var isHealthy = provider != null;
                    
                    if (isHealthy)
                    {
                        // Try to check provider health if it implements health check
                        // For now, just mark as healthy if provider exists
                        healthyProviders++;
                        healthResults[providerType.ToString()] = new
                        {
                            Status = "Healthy",
                            IsAvailable = true,
                            ProviderType = providerType.ToString()
                        };
                    }
                    else
                    {
                        failedProviders.Add(providerType.ToString());
                        healthResults[providerType.ToString()] = new
                        {
                            Status = "Unhealthy",
                            IsAvailable = false,
                            ProviderType = providerType.ToString()
                        };
                    }
                }
                catch (Exception providerEx)
                {
                    _logger.LogWarning(providerEx, "Provider {ProviderType} health check failed", providerType);
                    degradedProviders.Add(providerType.ToString());
                    healthResults[providerType.ToString()] = new
                    {
                        Status = "Degraded",
                        IsAvailable = false,
                        ProviderType = providerType.ToString(),
                        Error = providerEx.Message
                    };
                }
            }

            var summaryData = new Dictionary<string, object>
            {
                ["totalProviders"] = totalProviders,
                ["healthyProviders"] = healthyProviders,
                ["degradedProviders"] = degradedProviders.Count,
                ["failedProviders"] = failedProviders.Count,
                ["providers"] = healthResults
            };

            // Email provider is critical - if it's down, system is unhealthy
            var emailProviderHealthy = !failedProviders.Contains(ProviderType.Email.ToString()) && 
                                     !degradedProviders.Contains(ProviderType.Email.ToString());

            if (!emailProviderHealthy)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Critical email provider is unavailable. Failed: [{string.Join(", ", failedProviders)}], Degraded: [{string.Join(", ", degradedProviders)}]",
                    data: summaryData));
            }

            if (degradedProviders.Any() || failedProviders.Any())
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"{healthyProviders}/{totalProviders} notification providers are healthy. Failed: [{string.Join(", ", failedProviders)}], Degraded: [{string.Join(", ", degradedProviders)}]",
                    data: summaryData));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"All {healthyProviders} notification providers are healthy",
                data: summaryData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification providers health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check notification providers health", ex));
        }
    }
}