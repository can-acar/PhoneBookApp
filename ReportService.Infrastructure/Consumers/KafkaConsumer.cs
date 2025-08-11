using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportService.Domain.Interfaces;
using ReportService.Domain.Models.Kafka;

namespace ReportService.Infrastructure.Consumers;

/// <summary>
/// Kafka consumer implementation for Clean Architecture
/// Infrastructure layer: Handles external message queue integration
/// </summary>
public class KafkaConsumer : IKafkaConsumer, IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly ConsumerConfig _config;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _consumeTask;

    public KafkaConsumer(IOptions<ReportService.Infrastructure.Configuration.KafkaSettings> kafkaOptions, ILogger<KafkaConsumer> logger)
    {
        _logger = logger;
        var opts = kafkaOptions.Value;
        _config = new ConsumerConfig
        {
            BootstrapServers = string.IsNullOrWhiteSpace(opts.BootstrapServers) ? "localhost:9092" : opts.BootstrapServers,
            GroupId = string.IsNullOrWhiteSpace(opts.GroupId) ? "report-service-group" : opts.GroupId,
            ClientId = string.IsNullOrWhiteSpace(opts.ClientId) ? "report-service" : opts.ClientId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Manual offset management for exactly-once style processing
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<string, string>(_config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .SetLogHandler((_, e) => _logger.LogDebug("Kafka consumer log: {Level} {Name} {Facility}: {Message}",
                e.Level, e.Name, e.Facility, e.Message))
            .Build();

        _logger.LogInformation("Kafka consumer initialized with config: {@Config}", _config);
    }

    /// <summary>
    /// Modern async consumption method following Clean Architecture
    /// </summary>
    public async Task ConsumeAsync(string topic, Func<string, CancellationToken, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        try
        {
            _consumer.Subscribe(topic);
            _logger.LogInformation("Subscribed to Kafka topic: {Topic}", topic);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Block until a message is available or cancellation is requested
                    var consumeResult = _consumer.Consume(cancellationToken);

                    if (consumeResult?.Message != null)
                    {
                        _logger.LogDebug("Received message from topic {Topic}, partition {Partition}, offset {Offset}: {Message}",
                            consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumeResult.Message.Value);

                        // Process message using provided handler
                        await messageHandler(consumeResult.Message.Value, cancellationToken);

                        // Manual commit after successful processing
                        _consumer.StoreOffset(consumeResult);
                        _consumer.Commit(consumeResult);

                        _logger.LogDebug("Successfully processed and committed message from offset {Offset}", consumeResult.Offset);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from topic {Topic}: {Error}", topic, ex.Error.Reason);

                    // In production, implement dead letter queue logic here
                    await Task.Delay(1000, cancellationToken); // Backoff before retry
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka consumption cancelled for topic {Topic}", topic);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing message from topic {Topic}", topic);

                    // Continue processing other messages despite error
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Kafka consumer for topic {Topic}", topic);
            throw;
        }
        finally
        {
            try
            {
                _consumer.Unsubscribe();
                _logger.LogInformation("Unsubscribed from Kafka topic: {Topic}", topic);
                // Ensure graceful leave of the group and commit final offsets
                _consumer.Close();
                _logger.LogInformation("Kafka consumer closed for topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unsubscribing from topic {Topic}", topic);
            }
        }
    }

    /// <summary>
    /// Legacy consumption method for backward compatibility
    /// </summary>
    public void StartConsuming(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _consumeTask = Task.Run(async () =>
        {
            // Default to "report-requests" topic for legacy compatibility
            await ConsumeAsync("report-requests", async (message, ct) =>
            {
                _logger.LogInformation("Legacy processing: {Message}", message);

                // Basic message processing - extend as needed
                var reportRequest = JsonSerializer.Deserialize<ReportRequestMessage>(message);
                if (reportRequest != null)
                {
                    _logger.LogInformation("Processed legacy report request: {ReportId}", reportRequest.ReportId);
                    await Task.Delay(100, ct); // Simulate async work to satisfy warning
                }
            }, _cancellationTokenSource.Token);
        }, _cancellationTokenSource.Token);

        _logger.LogInformation("Started legacy Kafka consumption");
    }

    /// <summary>
    /// Stop consuming messages
    /// </summary>
    public void StopConsuming()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _consumeTask?.Wait(TimeSpan.FromSeconds(10));
            _logger.LogInformation("Stopped Kafka consumption");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping Kafka consumer");
        }
    }

    public void Dispose()
    {
        StopConsuming();
        _cancellationTokenSource?.Dispose();
        _consumer?.Dispose();
        _logger.LogInformation("Kafka consumer disposed");
    }
}