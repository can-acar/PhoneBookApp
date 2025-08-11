using Confluent.Kafka;
using ReportService.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReportService.Infrastructure.HealthChecks;

public class KafkaHealthCheck : IHealthCheck
{
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<KafkaHealthCheck> _logger;

    public KafkaHealthCheck(IOptions<KafkaSettings> kafkaOptions, ILogger<KafkaHealthCheck> logger)
    {
        _kafkaSettings = kafkaOptions.Value;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers
            };

            using var adminClient = new AdminClientBuilder(config).Build();
            
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            
            var brokerCount = metadata.Brokers.Count;
            var topicCount = metadata.Topics.Count;

            if (brokerCount == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("No Kafka brokers available"));
            }

            var data = new Dictionary<string, object>
            {
                ["brokerCount"] = brokerCount,
                ["topicCount"] = topicCount,
                ["bootstrapServers"] = _kafkaSettings.BootstrapServers
            };

            // Check if required topics exist
            var requiredTopics = new[] 
            { 
                _kafkaSettings.Topics.ReportRequests,
                _kafkaSettings.Topics.ReportCompleted
            };

            var missingTopics = requiredTopics.Where(topic => 
                !metadata.Topics.Any(t => t.Topic == topic)).ToList();

            if (missingTopics.Any())
            {
                data["missingTopics"] = missingTopics;
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Kafka is available but missing topics: {string.Join(", ", missingTopics)}", 
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"Kafka is healthy with {brokerCount} brokers", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka connection failed", ex));
        }
    }
}