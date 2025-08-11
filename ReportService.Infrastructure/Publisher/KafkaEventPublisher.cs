using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportService.Domain.Interfaces;
using ReportService.Infrastructure.Configuration;

namespace ReportService.Infrastructure.Publisher;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly KafkaSettings _kafkaSettings;

    public KafkaEventPublisher(
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<KafkaEventPublisher> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 10000,
            RequestTimeoutMs = 5000,
            EnableIdempotence = true,
            MaxInFlight = 1,
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("Producer error: {Error}", e.Reason))
            .SetLogHandler((_, message) =>
            {
                if (message.Level <= SyslogLevel.Warning)
                    _logger.LogWarning("Producer log: {Message}", message.Message);
            })
            .Build();
    }

    public async Task PublishAsync<T>(string topic, T eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            var serializedEvent = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = serializedEvent,
                Headers = new Headers
                {
                    { "content-type", System.Text.Encoding.UTF8.GetBytes("application/json") },
                    { "source", System.Text.Encoding.UTF8.GetBytes("ReportService") },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) }
                }
            };

            var deliveryReport = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Successfully published event to topic {Topic}, partition {Partition}, offset {Offset}",
                deliveryReport.Topic,
                deliveryReport.Partition.Value,
                deliveryReport.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish event to topic {Topic}. Error: {Error}",
                topic, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while publishing event to topic {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka producer");
        }
    }
}