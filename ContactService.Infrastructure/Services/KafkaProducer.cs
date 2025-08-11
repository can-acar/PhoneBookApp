using System.Text.Json;
using Confluent.Kafka;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactService.Infrastructure.Services;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly ICorrelationContext _correlationContext;
    private bool _disposed = false;

    public KafkaProducer(
        IOptions<KafkaSettings> kafkaOptions, 
        ILogger<KafkaProducer> logger,
        ICorrelationContext correlationContext)
    {
        _logger = logger;
        _correlationContext = correlationContext;
        var kafkaSettings = kafkaOptions.Value;
        
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            ClientId = kafkaSettings.ClientId,
            EnableIdempotence = kafkaSettings.ProducerConfig.EnableIdempotence,
            Acks = Enum.Parse<Acks>(kafkaSettings.ProducerConfig.Acks, true),
            MessageTimeoutMs = kafkaSettings.ProducerConfig.MessageTimeoutMs,
            RequestTimeoutMs = kafkaSettings.ProducerConfig.RequestTimeoutMs,
            RetryBackoffMs = kafkaSettings.ProducerConfig.RetryBackoffMs,
            MessageSendMaxRetries = kafkaSettings.ProducerConfig.Retries,
            BatchSize = kafkaSettings.ProducerConfig.BatchSize,
            LingerMs = kafkaSettings.ProducerConfig.LingerMs,
            CompressionType = Enum.Parse<CompressionType>(kafkaSettings.ProducerConfig.CompressionType, true),
            // Ensure message ordering
            MaxInFlight = 1
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
            {
                _logger.LogError("Kafka producer error: {Error}", e.Reason);
            })
            .SetLogHandler((_, logMessage) =>
            {
                var logLevel = logMessage.Level switch
                {
                    SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Error,
                    SyslogLevel.Warning => LogLevel.Warning,
                    SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                    SyslogLevel.Debug => LogLevel.Debug,
                    _ => LogLevel.Information
                };
                
                _logger.Log(logLevel, "Kafka: {Message}", logMessage.Message);
            })
            .Build();

        _logger.LogInformation("Kafka producer initialized with servers: {BootstrapServers}", 
            config.BootstrapServers);
    }

    public async Task PublishAsync(string topic, string message, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        // Use provided correlation ID or get from context
        var actualCorrelationId = correlationId ?? _correlationContext.CorrelationId;
        
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = actualCorrelationId,
                Value = message,
                Headers = new Headers
                {
                    { "CorrelationId", System.Text.Encoding.UTF8.GetBytes(actualCorrelationId) },
                    { "Source", System.Text.Encoding.UTF8.GetBytes("ContactService") },
                    { "Timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
                }
            };

            _logger.LogDebug("Publishing message to topic {Topic} with correlation ID {CorrelationId}", 
                topic, actualCorrelationId);

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

            _logger.LogInformation("Successfully published message to {Topic} at offset {Offset} with correlation ID {CorrelationId}",
                deliveryResult.Topic, deliveryResult.Offset.Value, actualCorrelationId);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic {Topic} with correlation ID {CorrelationId}. Error: {Error}",
                topic, actualCorrelationId, ex.Error.Reason);
            throw new InvalidOperationException($"Failed to publish message to Kafka: {ex.Error.Reason}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while publishing message to topic {Topic} with correlation ID {CorrelationId}",
                topic, actualCorrelationId);
            throw;
        }
    }

    public async Task PublishAsync<T>(string topic, T data, string? correlationId = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var jsonMessage = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await PublishAsync(topic, jsonMessage, correlationId, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize data of type {DataType} for topic {Topic}", 
                typeof(T).Name, topic);
            throw new InvalidOperationException($"Failed to serialize data: {ex.Message}", ex);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KafkaProducer));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _logger.LogInformation("Disposing Kafka producer...");
                
                // Flush any pending messages
                _producer?.Flush(TimeSpan.FromSeconds(10));
                _producer?.Dispose();
                
                _logger.LogInformation("Kafka producer disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while disposing Kafka producer");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}