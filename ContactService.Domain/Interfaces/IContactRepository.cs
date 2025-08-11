using ContactService.Domain.Entities;
using ContactService.Domain.Models;

namespace ContactService.Domain.Interfaces
{
    public interface IContactRepository
    {
        Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<(IEnumerable<Contact> contacts, int totalCount)> GetAllPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default);
        Task<(IEnumerable<Contact> contacts, int totalCount)> SearchAsync(int page, int pageSize, string searchTerm, CancellationToken cancellationToken = default);
        Task<(IEnumerable<Contact> contacts, int totalCount)> GetByLocationAsync(int page, int pageSize, string location, CancellationToken cancellationToken = default);
        Task<Contact?> CreateAsync(Contact? contact, CancellationToken cancellationToken = default);
        Task<Contact?> UpdateAsync(Contact? contact, CancellationToken cancellationToken = default);
        Task<bool>DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ContactInfo> AddContactInfoAsync(ContactInfo contactInfo, CancellationToken cancellationToken = default);
        Task<bool> RemoveContactInfoAsync(Guid contactInfoId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<LocationStatistic>> GetLocationStatisticsAsync(CancellationToken cancellationToken = default);
        
    }
}