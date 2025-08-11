using ContactService.Domain.Entities;

namespace ContactService.Domain.Interfaces
{
    public interface IContactHistoryRepository
    {
        Task<ContactHistory> CreateAsync(ContactHistory contactHistory, CancellationToken cancellationToken = default);
        Task<ContactHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ContactHistory>> GetByContactIdAsync(Guid contactId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ContactHistory>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ContactHistory>> GetByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default);
        Task<IEnumerable<ContactHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<ContactHistory>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetCountByContactIdAsync(Guid contactId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        
        // Replay functionality
        Task<IEnumerable<ContactHistory>> GetContactHistoryForReplayAsync(
            Guid contactId, 
            DateTime? fromTimestamp = null, 
            CancellationToken cancellationToken = default);
        
        // Cleanup functionality for old audit records
        Task<int> DeleteOldHistoryRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    }
}