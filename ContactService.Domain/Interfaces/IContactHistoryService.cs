using ContactService.Domain.Entities;

namespace ContactService.Domain.Interfaces
{
    public interface IContactHistoryService
    {
        Task RecordContactHistoryAsync(
            Guid contactId, 
            string operationType, 
            object contactData, 
            string correlationId,
            string? userId = null,
            CancellationToken cancellationToken = default);

        Task<ContactHistory?> GetHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default);
        
        Task<IEnumerable<ContactHistory>> GetContactHistoryAsync(Guid contactId, CancellationToken cancellationToken = default);
        
        Task<IEnumerable<ContactHistory>> GetHistoryByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
        
        Task<IEnumerable<ContactHistory>> GetHistoryByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default);
        
        Task<IEnumerable<ContactHistory>> GetHistoryByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        
        // Replay functionality
        Task<Contact?> ReplayContactStateAsync(Guid contactId, DateTime? pointInTime = null, CancellationToken cancellationToken = default);
        
        Task<IEnumerable<ContactHistory>> GetAuditTrailAsync(Guid contactId, CancellationToken cancellationToken = default);
        
        // Cleanup functionality
        Task<int> CleanupOldHistoryAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
    }
}