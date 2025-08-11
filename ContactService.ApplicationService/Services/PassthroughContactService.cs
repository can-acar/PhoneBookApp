using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Models;
using Shared.CrossCutting.Models;

namespace ContactService.ApplicationService.Services;

// Adaptor: IContactService çağrılarını doğrudan IContactServiceCore'a yönlendirir (cache yokken kullanılır)
public class PassthroughContactService : IContactService
{
    private readonly IContactServiceCore _core;

    public PassthroughContactService(IContactServiceCore core)
    {
        _core = core;
    }

    public Task<Contact?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _core.GetContactByIdAsync(id, cancellationToken);

    public Task<Pagination<Contact>> GetAllContactsAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
        => _core.GetAllContactsAsync(page, pageSize, searchTerm, cancellationToken);

    public Task<Pagination<Contact>> GetContactsFilterByCompany(int page, int pageSize, string? company = null, CancellationToken cancellationToken = default)
        => _core.GetContactsFilterByCompany(page, pageSize, company, cancellationToken);

    public Task<Pagination<Contact>> GetContactsFilterByLocation(int page, int pageSize, string? location = null, CancellationToken cancellationToken = default)
        => _core.GetContactsFilterByLocation(page, pageSize, location, cancellationToken);

    public Task<Contact?> CreateContactAsync(string firstName, string lastName, string company, IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default)
        => _core.CreateContactAsync(firstName, lastName, company, contactInfos, cancellationToken);

    public Task<Contact?> UpdateContactAsync(Guid id, string firstName, string lastName, string company, IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default)
        => _core.UpdateContactAsync(id, firstName, lastName, company, contactInfos, cancellationToken);

    public Task<bool> DeleteContactAsync(Guid id, CancellationToken cancellationToken = default)
        => _core.DeleteContactAsync(id, cancellationToken);

    public Task<Contact?> AddContactInfoAsync(Guid contactId, int infoType, string infoValue, CancellationToken cancellationToken = default)
        => _core.AddContactInfoAsync(contactId, infoType, infoValue, cancellationToken);

    public Task<bool> RemoveContactInfoAsync(Guid contactId, Guid contactInfoId, CancellationToken cancellationToken = default)
        => _core.RemoveContactInfoAsync(contactId, contactInfoId, cancellationToken);

    public Task<bool> ContactExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _core.ContactExistsAsync(id, cancellationToken);

    public Task<List<LocationStatistic>> GetLocationStatistics(CancellationToken cancellationToken)
        => _core.GetLocationStatistics(cancellationToken);
}
