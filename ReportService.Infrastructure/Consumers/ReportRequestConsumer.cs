using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using ReportService.Domain.Interfaces;
using ReportService.Domain.Models.Kafka;
using ReportService.Infrastructure.Configuration;

namespace ReportService.Infrastructure.Consumers;

/// <summary>
/// Legacy Kafka consumer implementation for backward compatibility
/// Clean Architecture: Infrastructure layer consumer for Kafka integration
/// </summary>
public class ReportRequestConsumer : IKafkaConsumer
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IReportGenerationService _reportGenerationService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<ReportRequestConsumer> _logger;
    private readonly KafkaSettings _settings;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _consumingTask;

    public ReportRequestConsumer(
        KafkaSettings settings,
        IReportGenerationService reportGenerationService,
        IKafkaProducer kafkaProducer,
        ILogger<ReportRequestConsumer> logger)
    {
        _settings = settings;
        _reportGenerationService = reportGenerationService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
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
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult?.Message != null)
                    {
                        _logger.LogDebug("Received message from topic {Topic}: {Message}",
                            topic, consumeResult.Message.Value);

                        // Process message using provided handler
                        await messageHandler(consumeResult.Message.Value, cancellationToken);

                        // Manual commit after successful processing
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from topic {Topic}", topic);
                    await Task.Delay(1000, cancellationToken); // Backoff
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        finally
        {
            try
            {
                _consumer.Unsubscribe();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unsubscribing from topic {Topic}", topic);
            }
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public void StartConsuming(CancellationToken cancellationToken = default)
    {
        _consumer.Subscribe(_settings.Topics.ReportRequests);
        _logger.LogInformation("Started consuming from topic: {Topic}", _settings.Topics.ReportRequests);

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _consumingTask = Task.Run(async () => await ConsumeMessages(_cancellationTokenSource.Token), cancellationToken);
    }

    public void StopConsuming()
    {
        _cancellationTokenSource?.Cancel();
        _consumingTask?.Wait();
        _consumer.Close();
        _consumer.Dispose();
        _logger.LogInformation("Stopped consuming from topic: {Topic}", _settings.Topics.ReportRequests);
    }

    private async Task ConsumeMessages(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(cancellationToken);
                    if (consumeResult == null) continue;

                    string? correlationId = null;
                    var correlationIdHeader = consumeResult.Message.Headers.FirstOrDefault(h => h.Key == "X-Correlation-ID");
                    if (correlationIdHeader != null)
                    {
                        correlationId = Encoding.UTF8.GetString(correlationIdHeader.GetValueBytes());
                    }

                    _logger.LogInformation(
                        "Received report request message: {MessageKey} with CorrelationId: {CorrelationId}",
                        consumeResult.Message.Key,
                        correlationId ?? "none");

                    var reportRequestMessage = JsonSerializer.Deserialize<ReportRequestMessage>(consumeResult.Message.Value);
                    if (reportRequestMessage != null)
                    {
                        await ProcessReportRequestAsync(reportRequestMessage, correlationId, cancellationToken);
                    }

                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Kafka message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consuming was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Kafka consumer");
        }
    }

    private async Task ProcessReportRequestAsync(ReportRequestMessage message, string? correlationId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing report request: {ReportId}", message.ReportId);

            // Use the new interface method with location and user context
            await _reportGenerationService.GenerateReportAsync(
                message.ReportId,
                message.Location,
                message.UserId,
                cancellationToken);

            // Publish completion message
            await _kafkaProducer.PublishReportCompletedAsync(new ReportCompletedMessage
            {
                ReportId = message.ReportId,
                CompletedAt = DateTime.UtcNow,
                Success = true,
                CorrelationId = correlationId,
                // Additional data could be added here if needed
            }, cancellationToken);

            _logger.LogInformation("Report generation completed for: {ReportId}", message.ReportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing report request: {ReportId}", message.ReportId);

            // Publish failure message
            await _kafkaProducer.PublishReportCompletedAsync(new ReportCompletedMessage
            {
                ReportId = message.ReportId,
                CompletedAt = DateTime.UtcNow,
                Success = false,
                ErrorMessage = ex.Message,
                CorrelationId = correlationId
            }, cancellationToken);
        }
    }
}