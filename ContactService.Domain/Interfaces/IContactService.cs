using ContactService.Domain.Entities;
using ContactService.Domain.Models;
using Shared.CrossCutting.Models;

namespace ContactService.Domain.Interfaces
{
    public interface IContactService
    {
    
        Task<Contact?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>> GetAllContactsAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>>  GetContactsFilterByCompany(int page, int pageSize, string? company = null, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>>  GetContactsFilterByLocation(int page, int pageSize, string? location = null, CancellationToken cancellationToken = default);
        
        
        // Create and Update operations
        Task<Contact?> CreateContactAsync(string firstName,string lastName, string company, IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default);
        Task<Contact?> UpdateContactAsync(Guid id, string firstName,string lastName, string company,IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default);
        Task<bool> DeleteContactAsync(Guid id, CancellationToken cancellationToken = default);
        
        // Contact Info operations
        Task<Contact?> AddContactInfoAsync(Guid contactId, int infoType, string infoValue, CancellationToken cancellationToken = default);
        Task<bool> RemoveContactInfoAsync(Guid contactId, Guid contactInfoId, CancellationToken cancellationToken = default);
        
        // Utility methods
        Task<bool> ContactExistsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<List<LocationStatistic>> GetLocationStatistics(CancellationToken cancellationToken);
    }

    // Base service contract implemented by the core service (non-cached).
    // DO NOT inherit from IContactService to avoid DI resolving back to the decorated type.
    // It intentionally duplicates the contract to keep the undecorated surface separate.
    public interface IContactServiceCore
    {
        Task<Contact?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>> GetAllContactsAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>>  GetContactsFilterByCompany(int page, int pageSize, string? company = null, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>>  GetContactsFilterByLocation(int page, int pageSize, string? location = null, CancellationToken cancellationToken = default);

        Task<Contact?> CreateContactAsync(string firstName,string lastName, string company, IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default);
        Task<Contact?> UpdateContactAsync(Guid id, string firstName,string lastName, string company,IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default);
        Task<bool> DeleteContactAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Contact?> AddContactInfoAsync(Guid contactId, int infoType, string infoValue, CancellationToken cancellationToken = default);
        Task<bool> RemoveContactInfoAsync(Guid contactId, Guid contactInfoId, CancellationToken cancellationToken = default);

        Task<bool> ContactExistsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<List<LocationStatistic>> GetLocationStatistics(CancellationToken cancellationToken);
    }
    public interface ICachedContactService
    {
    
        Task<Contact?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>> GetAllContactsAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>>  GetContactsFilterByCompany(int page, int pageSize, string? company = null, CancellationToken cancellationToken = default);
        Task<Pagination<Contact>>  GetContactsFilterByLocation(int page, int pageSize, string? location = null, CancellationToken cancellationToken = default);
        
        
        // Create and Update operations
        Task<Contact?> CreateContactAsync(string firstName,string lastName, string company, IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default);
        Task<Contact?> UpdateContactAsync(Guid id, string firstName,string lastName, string company,IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default);
        Task<bool> DeleteContactAsync(Guid id, CancellationToken cancellationToken = default);
        
        // Contact Info operations
        Task<Contact?> AddContactInfoAsync(Guid contactId, int infoType, string infoValue, CancellationToken cancellationToken = default);
        Task<bool> RemoveContactInfoAsync(Guid contactId, Guid contactInfoId, CancellationToken cancellationToken = default);
        
        // Utility methods
        Task<bool> ContactExistsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<List<LocationStatistic>> GetLocationStatistics(CancellationToken cancellationToken);
    }
}
