using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactService.ApplicationService.Services;

public class OutboxService : IOutboxService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(
        IOutboxRepository outboxRepository,
        IKafkaProducer kafkaProducer,
        IOptions<KafkaSettings> kafkaOptions,
        ILogger<OutboxService> logger)
    {
        _outboxRepository = outboxRepository;
        _kafkaProducer = kafkaProducer;
        _kafkaSettings = kafkaOptions.Value;
        _logger = logger;
    }

    public async Task AddEventAsync<T>(string eventType, T eventData, string correlationId, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var outboxEvent = new OutboxEvent(eventType, eventData, correlationId);
            await _outboxRepository.CreateAsync(outboxEvent, cancellationToken);
            
            _logger.LogInformation("Added event {EventType} to outbox with correlation ID {CorrelationId}", 
                eventType, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add event {EventType} to outbox with correlation ID {CorrelationId}", 
                eventType, correlationId);
            throw;
        }
    }

    public async Task ProcessPendingEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingEvents = await _outboxRepository.GetPendingEventsAsync(50, cancellationToken);
            var eventsList = pendingEvents.ToList();
            
            if (!eventsList.Any())
            {
                _logger.LogDebug("No pending outbox events to process");
                return;
            }

            _logger.LogInformation("Processing {Count} pending outbox events", eventsList.Count);

            foreach (var outboxEvent in eventsList)
            {
                await ProcessSingleEventAsync(outboxEvent, cancellationToken);
            }

            _logger.LogInformation("Completed processing {Count} outbox events", eventsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing pending outbox events");
            throw;
        }
    }

    public async Task ProcessFailedEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var failedEvents = await _outboxRepository.GetFailedEventsReadyForRetryAsync(50, cancellationToken);
            var eventsList = failedEvents.ToList();
            
            if (!eventsList.Any())
            {
                _logger.LogDebug("No failed outbox events ready for retry");
                return;
            }

            _logger.LogInformation("Retrying {Count} failed outbox events", eventsList.Count);

            foreach (var outboxEvent in eventsList)
            {
                outboxEvent.ResetForRetry();
                await _outboxRepository.UpdateAsync(outboxEvent, cancellationToken);
                await ProcessSingleEventAsync(outboxEvent, cancellationToken);
            }

            _logger.LogInformation("Completed retrying {Count} failed outbox events", eventsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing failed outbox events");
            throw;
        }
    }

    public async Task CleanupProcessedEventsAsync(int retentionDays = 7, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            await _outboxRepository.DeleteProcessedEventsAsync(cutoffDate, cancellationToken);
            
            _logger.LogInformation("Cleaned up processed outbox events older than {CutoffDate}", cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cleaning up processed outbox events");
            throw;
        }
    }

    public async Task<OutboxEventStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingCount = await _outboxRepository.GetPendingEventCountAsync(cancellationToken);
            var failedCount = await _outboxRepository.GetFailedEventCountAsync(cancellationToken);

            return new OutboxEventStatistics
            {
                PendingEvents = pendingCount,
                FailedEvents = failedCount,
                ProcessedEventsToday = 0, // TODO: Implement if needed
                LastProcessedAt = DateTime.UtcNow // TODO: Track actual last processed time
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting outbox event statistics");
            throw;
        }
    }

    private async Task ProcessSingleEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing outbox event {EventId} of type {EventType}", 
                outboxEvent.Id, outboxEvent.EventType);

            // Determine the Kafka topic based on event type
            var topic = GetTopicForEventType(outboxEvent.EventType);
            
            // Publish to Kafka
            await _kafkaProducer.PublishAsync(topic, outboxEvent.EventData, outboxEvent.CorrelationId, cancellationToken);

            // Mark as processed
            outboxEvent.MarkAsProcessed();
            await _outboxRepository.UpdateAsync(outboxEvent, cancellationToken);

            _logger.LogInformation("Successfully processed outbox event {EventId} of type {EventType}", 
                outboxEvent.Id, outboxEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process outbox event {EventId} of type {EventType}", 
                outboxEvent.Id, outboxEvent.EventType);

            try
            {
                outboxEvent.MarkAsFailed($"Failed to publish to Kafka: {ex.Message}");
                await _outboxRepository.UpdateAsync(outboxEvent, cancellationToken);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update outbox event {EventId} status after failure", 
                    outboxEvent.Id);
            }
        }
    }

    private string GetTopicForEventType(string eventType)
    {
        return eventType switch
        {
            "ContactCreated" => _kafkaSettings.Topics.ContactEvents,
            "ContactUpdated" => _kafkaSettings.Topics.ContactEvents,
            "ContactDeleted" => _kafkaSettings.Topics.ContactEvents,
            "ReportRequested" => _kafkaSettings.Topics.ReportEvents,
            "ReportCompleted" => _kafkaSettings.Topics.ReportEvents,
            "NotificationSent" => _kafkaSettings.Topics.NotificationEvents,
            _ => _kafkaSettings.Topics.ContactEvents // Default to contact events
        };
    }
}