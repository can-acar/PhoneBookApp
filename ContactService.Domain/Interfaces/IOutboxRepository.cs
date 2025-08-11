using ContactService.Domain.Entities;

namespace ContactService.Domain.Interfaces;

public interface IOutboxRepository
{
    Task<OutboxEvent> CreateAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    Task<OutboxEvent> UpdateAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    Task<OutboxEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync(int batchSize = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxEvent>> GetFailedEventsReadyForRetryAsync(int batchSize = 50, CancellationToken cancellationToken = default);
    Task DeleteProcessedEventsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task<int> GetPendingEventCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetFailedEventCountAsync(CancellationToken cancellationToken = default);
}