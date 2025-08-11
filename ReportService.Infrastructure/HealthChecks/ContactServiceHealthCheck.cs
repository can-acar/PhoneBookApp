using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace ReportService.Infrastructure.HealthChecks;

public class ContactServiceHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContactServiceHealthCheck> _logger;
    private readonly string _contactServiceUrl;

    public ContactServiceHealthCheck(HttpClient httpClient, ILogger<ContactServiceHealthCheck> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _contactServiceUrl = configuration.GetValue<string>("ContactService:BaseUrl") ?? "http://localhost:5000";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthEndpoint = $"{_contactServiceUrl}/health/ready";
            
            using var response = await _httpClient.GetAsync(healthEndpoint, cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["endpoint"] = healthEndpoint,
                ["statusCode"] = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Contact Service is available", data);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                return HealthCheckResult.Degraded("Contact Service is degraded", exception: null, data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy($"Contact Service returned {response.StatusCode}", exception: null, data: data);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Contact Service health check failed - service may be down");
            return HealthCheckResult.Unhealthy("Contact Service is not reachable", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Contact Service health check timed out");
            return HealthCheckResult.Unhealthy("Contact Service health check timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact Service health check failed");
            return HealthCheckResult.Unhealthy("Contact Service health check failed", ex);
        }
    }
}