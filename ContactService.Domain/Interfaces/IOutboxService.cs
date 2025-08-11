namespace ContactService.Domain.Interfaces;

public interface IOutboxService
{
    Task AddEventAsync<T>(string eventType, T eventData, string correlationId, CancellationToken cancellationToken = default) where T : class;
    Task ProcessPendingEventsAsync(CancellationToken cancellationToken = default);
    Task ProcessFailedEventsAsync(CancellationToken cancellationToken = default);
    Task CleanupProcessedEventsAsync(int retentionDays = 7, CancellationToken cancellationToken = default);
    Task<OutboxEventStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public class OutboxEventStatistics
{
    public int PendingEvents { get; set; }
    public int FailedEvents { get; set; }
    public int ProcessedEventsToday { get; set; }
    public DateTime LastProcessedAt { get; set; }
}