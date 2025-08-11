using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportService.Domain.Enums;
using ReportService.Domain.Interfaces;
using ReportService.Domain.Models.Kafka;
using ReportService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ReportService.Infrastructure.Consumers;

public class ReportCompletedConsumer : IHostedService
{
    private readonly KafkaSettings _settings;
    private readonly IReportService _reportService;
    private readonly ILogger<ReportCompletedConsumer> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _consumingTask;
    private readonly IConsumer<string, string> _consumer;

    public ReportCompletedConsumer(
        IOptions<KafkaSettings> settings,
        IReportService reportService,
        ILogger<ReportCompletedConsumer> logger)
    {
        _settings = settings.Value;
        _reportService = reportService;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = $"{_settings.GroupId}-completed",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
    _consumer.Subscribe(_settings.Topics.ReportCompleted);
        _logger.LogInformation("Started consuming from topic: {Topic}", _settings.Topics.ReportCompleted);
        
        _consumingTask = Task.Run(async () => await ConsumeMessages(_cancellationTokenSource.Token), cancellationToken);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        
        if (_consumingTask != null)
        {
            try
            {
                await _consumingTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consuming was cancelled");
            }
        }
        
        _consumer.Close();
        _consumer.Dispose();
        _logger.LogInformation("Stopped consuming from topic: {Topic}", _settings.Topics.ReportCompleted);
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
                        "Received report completed message: {MessageKey} with CorrelationId: {CorrelationId}",
                        consumeResult.Message.Key,
                        correlationId ?? "none");

                    var reportCompletedMessage = JsonSerializer.Deserialize<ReportCompletedMessage>(consumeResult.Message.Value);
                    if (reportCompletedMessage != null)
                    {
                        await ProcessReportCompletedAsync(reportCompletedMessage, correlationId, cancellationToken);
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

    private async Task ProcessReportCompletedAsync(ReportCompletedMessage message, string? correlationId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing report completed notification: {ReportId}", message.ReportId);
            
            // This consumer is just an example of how we might handle report completion events
            // In a real implementation, you might want to send notifications, update dashboards, etc.
            var report = await _reportService.GetByIdAsync(message.ReportId, cancellationToken);
            
            if (report == null)
            {
                _logger.LogWarning("Report {ReportId} not found", message.ReportId);
                return;
            }
            
            if (!report.CompletedAt.HasValue)
            {
                _logger.LogWarning("Report {ReportId} completed message received but report is not marked as completed in database", message.ReportId);
                
                // Update report status based on the message
                if (message.Success)
                {
                    report.MarkAsCompleted(
                        message.PersonCount,
                        message.PhoneNumberCount);
                }
                else
                {
                    report.MarkAsFailed(message.ErrorMessage ?? "Unknown error occurred");
                }
                
                await _reportService.UpdateAsync(report, cancellationToken);
                _logger.LogInformation("Report {ReportId} status updated to {Status}", message.ReportId, report.Status);
            }
            else
            {
                _logger.LogInformation("Report {ReportId} is already marked as completed in database", message.ReportId);
            }
            
            // Here you could add additional logic like:
            // 1. Send email notifications
            // 2. Update UI via SignalR
            // 3. Generate downloadable report files
            // 4. Archive old reports
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing report completed notification: {ReportId}", message.ReportId);
        }
    }
}
