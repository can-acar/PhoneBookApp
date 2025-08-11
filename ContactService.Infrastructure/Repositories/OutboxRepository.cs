using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContactService.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly ContactDbContext _context;

    public OutboxRepository(ContactDbContext context)
    {
        _context = context;
    }

    public async Task<OutboxEvent> CreateAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync(cancellationToken);
        return outboxEvent;
    }

    public async Task<OutboxEvent> UpdateAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        _context.OutboxEvents.Update(outboxEvent);
        await _context.SaveChangesAsync(cancellationToken);
        return outboxEvent;
    }

    public async Task<OutboxEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .Where(e => e.Status == OutboxEventStatus.Pending && 
                       (e.NextRetryAt == null || e.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OutboxEvent>> GetFailedEventsReadyForRetryAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .Where(e => e.Status == OutboxEventStatus.Failed && 
                       e.RetryCount < 5 &&
                       e.NextRetryAt != null && 
                       e.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(e => e.NextRetryAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteProcessedEventsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var processedEvents = await _context.OutboxEvents
            .Where(e => e.Status == OutboxEventStatus.Processed && 
                       e.ProcessedAt != null && 
                       e.ProcessedAt < olderThan)
            .ToListAsync(cancellationToken);

        if (processedEvents.Any())
        {
            _context.OutboxEvents.RemoveRange(processedEvents);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetPendingEventCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .CountAsync(e => e.Status == OutboxEventStatus.Pending, cancellationToken);
    }

    public async Task<int> GetFailedEventCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .CountAsync(e => e.Status == OutboxEventStatus.Failed, cancellationToken);
    }
}