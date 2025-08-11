using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportService.Domain.Events;
using ReportService.Domain.Interfaces;
using ReportService.Infrastructure.Configuration;

namespace ReportService.ApplicationService.Worker;

public class ReportProcessingWorker : BackgroundService
{
    private readonly ILogger<ReportProcessingWorker> _logger;
    private readonly IServiceProvider _sp;
    private readonly KafkaSettings _kafkaSettings;
    private IConsumer<string, string>? _consumer;
    private readonly ConsumerConfig _consumerConfig;

    public ReportProcessingWorker(
        ILogger<ReportProcessingWorker> logger,
        IServiceProvider sp,
        IOptions<KafkaSettings> kafka)
    {
        _logger = logger;
        _sp = sp;
        _kafkaSettings = kafka.Value;
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false,
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Kafka consumer for Report Processing");

        await Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Error}", e.Reason))
                .SetLogHandler((_, e) => _logger.LogDebug("Kafka consumer log: {Level} {Name} {Facility}: {Message}",
                    e.Level, e.Name, e.Facility, e.Message))
                .Build();

            consumer.Subscribe(_kafkaSettings.Topics.ReportEvents);

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
                        await ProcessMessage(consumeResult.Message.Value, stoppingToken);

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


    private async Task ProcessMessage(string messageValue, CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IReportGenerationService>();

            using var doc = JsonDocument.Parse(messageValue);
            if (!doc.RootElement.TryGetProperty("eventType", out var et))
            {
                _logger.LogWarning("Missing eventType: {Message}", messageValue);
                return;
            }

            switch (et.GetString())
            {
                case "ReportRequested":
                    var e1 = JsonSerializer.Deserialize<ReportRequestedEvent>(messageValue);
                    if (e1 is not null)
                        await reportService.GenerateReportAsync(e1.ReportId, ct);
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", et.GetString());
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Deserialize failed: {Message}", messageValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Process failed: {Message}", messageValue);
            // İsteğe bağlı: DLQ / retry
        }
    }
}