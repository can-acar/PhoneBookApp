using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportService.Domain.Interfaces;
using ReportService.Domain.Models.Kafka;
using ReportService.Infrastructure.Configuration;

namespace ReportService.ApplicationService.Worker;

/// <summary>
/// Background service that consumes report generation requests from Kafka
/// Clean Architecture: Infrastructure layer handling external message queue
/// </summary>
public class ReportConsumerWorker : BackgroundService
{
    private readonly ILogger<ReportConsumerWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _kafkaSettings;
    private IConsumer<string, string>? _consumer;
    private readonly ConsumerConfig _consumerConfig;

    public ReportConsumerWorker(ILogger<ReportConsumerWorker> logger, IServiceProvider sp, IOptions<KafkaSettings> kafka)
    {
        _logger = logger;
        _serviceProvider = sp;
        _kafkaSettings = kafka.Value;
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.GroupId,
            ClientId = _kafkaSettings.ClientId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Kafka consumer for Report Consumer");

        await Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Error}", e.Reason))
                .SetLogHandler((_, e) => _logger.LogDebug("Kafka consumer log: {Level} {Name} {Facility}: {Message}",
                    e.Level, e.Name, e.Facility, e.Message))
                .Build();
            var topic = _kafkaSettings.Topics.ReportRequests;
            _logger.LogInformation("Starting consumption on topic: {Topic}", topic);
            consumer.Subscribe(topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);

                        if (consumeResult == null)
                        {
                            continue;
                        }

                        _logger.LogInformation(
                            "Received report completed event: {Key}, Partition: {Partition}, Offset: {Offset}",
                            consumeResult.Message.Key, consumeResult.Partition, consumeResult.Offset);

                        // Process the message
                        await ProcessReportRequestAsync(consumeResult.Message.Value, stoppingToken);

                        // Commit the offset after processing
                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error consuming Kafka message");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The token has been canceled
                _logger.LogInformation("Report completed consumer is shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Kafka consumer");
            }
            finally
            {
                consumer.Close();
            }
        }, stoppingToken);
    }


    /// <summary>
    /// Process individual report generation request
    /// Clean Architecture: Orchestrates domain services through application layer
    /// </summary>
    private async Task ProcessReportRequestAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing report request: {Message}", message);


            using var scope = _serviceProvider.CreateScope();
            var reportGenerationService = scope.ServiceProvider.GetRequiredService<IReportGenerationService>();

            // Parse the incoming message - ApiContract layer responsibility
            ReportRequestMessage? reportRequest;
            try
            {
                reportRequest = JsonSerializer.Deserialize<ReportRequestMessage>(message);
            }
            catch (JsonException)
            {
                _logger.LogWarning("Invalid report request message format: {Message}", message);
                return;
            }

            if (reportRequest == null)
            {
                _logger.LogWarning("Invalid report request message format: {Message}", message);
                return;
            }

            // Generate report asynchronously - Domain logic
            await reportGenerationService.GenerateReportAsync(
                reportRequest.ReportId,
                reportRequest.Location,
                reportRequest.UserId,
                cancellationToken);

            _logger.LogInformation("Report generation completed successfully for ReportId: {ReportId}", reportRequest.ReportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing report request: {Message}", message);
            // In production, this should go to dead letter queue
            throw;
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Report consumer worker is stopping");
        return base.StopAsync(cancellationToken);
    }
}