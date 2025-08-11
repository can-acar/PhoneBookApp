using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContactService.Infrastructure.Repositories
{
    public class ContactHistoryRepository : IContactHistoryRepository
    {
        private readonly ContactDbContext _context;

        public ContactHistoryRepository(ContactDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ContactHistory> CreateAsync(ContactHistory contactHistory, CancellationToken cancellationToken = default)
        {
            if (contactHistory == null)
                throw new ArgumentNullException(nameof(contactHistory));

            _context.ContactHistories.Add(contactHistory);
            await _context.SaveChangesAsync(cancellationToken);
            
            return contactHistory;
        }

        public async Task<ContactHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ContactHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetByContactIdAsync(Guid contactId, CancellationToken cancellationToken = default)
        {
            return await _context.ContactHistories
                .AsNoTracking()
                .Where(h => h.ContactId == contactId)
                .OrderBy(h => h.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

            return await _context.ContactHistories
                .AsNoTracking()
                .Where(h => h.CorrelationId == correlationId)
                .OrderBy(h => h.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(operationType))
                throw new ArgumentException("Operation type cannot be null or empty", nameof(operationType));

            return await _context.ContactHistories
                .AsNoTracking()
                .Where(h => h.OperationType == operationType.ToUpperInvariant())
                .OrderByDescending(h => h.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            if (startDate >= endDate)
                throw new ArgumentException("Start date must be before end date");

            return await _context.ContactHistories
                .AsNoTracking()
                .Where(h => h.Timestamp >= startDate && h.Timestamp <= endDate)
                .OrderByDescending(h => h.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
        {
            if (skip < 0)
                throw new ArgumentException("Skip must be non-negative", nameof(skip));
            
            if (take <= 0 || take > 1000)
                throw new ArgumentException("Take must be between 1 and 1000", nameof(take));

            return await _context.ContactHistories
                .AsNoTracking()
                .OrderByDescending(h => h.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ContactHistories.CountAsync(cancellationToken);
        }

        public async Task<int> GetCountByContactIdAsync(Guid contactId, CancellationToken cancellationToken = default)
        {
            return await _context.ContactHistories
                .CountAsync(h => h.ContactId == contactId, cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ContactHistories
                .AnyAsync(h => h.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetContactHistoryForReplayAsync(
            Guid contactId, 
            DateTime? fromTimestamp = null, 
            CancellationToken cancellationToken = default)
        {
            var query = _context.ContactHistories
                .AsNoTracking()
                .Where(h => h.ContactId == contactId);

            if (fromTimestamp.HasValue)
            {
                query = query.Where(h => h.Timestamp >= fromTimestamp.Value);
            }

            return await query
                .OrderBy(h => h.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> DeleteOldHistoryRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var oldRecords = await _context.ContactHistories
                .Where(h => h.Timestamp < olderThan)
                .ToListAsync(cancellationToken);

            if (oldRecords.Any())
            {
                _context.ContactHistories.RemoveRange(oldRecords);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return oldRecords.Count;
        }
    }
}