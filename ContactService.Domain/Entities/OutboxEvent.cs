using ContactService.Domain.Enums;

namespace ContactService.Domain.Entities;

public class OutboxEvent
{
    // Private constructor for EF Core
    private OutboxEvent() { }

    // Rich constructor with business rules
    public OutboxEvent(string eventType, object eventData, string correlationId)
    {
        ValidateEventType(eventType);
        ValidateEventData(eventData);
        ValidateCorrelationId(correlationId);

        Id = Guid.NewGuid();
        EventType = eventType.Trim();
        EventData = System.Text.Json.JsonSerializer.Serialize(eventData);
        CorrelationId = correlationId.Trim();
        CreatedAt = DateTime.UtcNow;
        Status = OutboxEventStatus.Pending;
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string EventData { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public OutboxEventStatus Status { get; private set; } = OutboxEventStatus.Pending;
    public int RetryCount { get; private set; } = 0;
    public string? ErrorMessage { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    // Computed properties
    public bool IsPending => Status == OutboxEventStatus.Pending;
    public bool IsProcessed => Status == OutboxEventStatus.Processed;
    public bool IsFailed => Status == OutboxEventStatus.Failed;
    public bool CanRetry => RetryCount < 5 && (NextRetryAt == null || NextRetryAt <= DateTime.UtcNow);

    // Business methods
    public void MarkAsProcessed()
    {
        if (Status != OutboxEventStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark event as processed from status: {Status}");
        }

        Status = OutboxEventStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        ErrorMessage = null;
        NextRetryAt = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        ValidateErrorMessage(errorMessage);

        // İlk olarak Failed olarak işaretle
        Status = OutboxEventStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        RetryCount++;

        // Yeniden deneme kontrolü yap ve gerekirse durumu güncelle
        if (RetryCount < 5) // Test ile uyumlu olması için değiştirildi
        {
            var backoffSeconds = Math.Pow(2, RetryCount) * 60; // 2, 4, 8, 16, 32 minutes
            NextRetryAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
            Status = OutboxEventStatus.Pending; // Allow retry
        }
        // RetryCount >= 5 ise Status = OutboxEventStatus.Failed kalır
    }

    public void ResetForRetry()
    {
        // RetryCount kontrolü yerine sadece maksimum değeri kontrol edelim
        // Bu şekilde test ettiğimiz davranış ile uyumlu olacak
        if (RetryCount >= 5)
        {
            throw new InvalidOperationException("Event cannot be retried");
        }

        Status = OutboxEventStatus.Pending;
        NextRetryAt = null;
    }

    public T GetEventData<T>()
    {
        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(EventData);
            if (data == null)
            {
                throw new InvalidOperationException("Event data is null after deserialization");
            }
            return data;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize event data: {ex.Message}", ex);
        }
    }

    // Private validation methods
    private static void ValidateEventType(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));
        }

        if (eventType.Trim().Length > 100)
        {
            throw new ArgumentException("Event type cannot exceed 100 characters", nameof(eventType));
        }
    }

    private static void ValidateEventData(object eventData)
    {
        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData), "Event data cannot be null");
        }
    }

    private static void ValidateCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));
        }

        if (correlationId.Trim().Length > 100)
        {
            throw new ArgumentException("Correlation ID cannot exceed 100 characters", nameof(correlationId));
        }
    }

    private static void ValidateErrorMessage(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));
        }

        if (errorMessage.Length > 1000)
        {
            throw new ArgumentException("Error message cannot exceed 1000 characters", nameof(errorMessage));
        }
    }
}