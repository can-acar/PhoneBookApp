using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface IContactServiceClient : IDisposable
{
    Task<IEnumerable<ContactSummary>> GetContactsByLocationAsync(string location, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContactSummary>> GetAllContactsAsync(CancellationToken cancellationToken = default);
}
