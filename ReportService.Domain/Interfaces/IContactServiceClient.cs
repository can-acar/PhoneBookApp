using ReportService.Domain.Entities;

namespace ReportService.Domain.Interfaces;

public interface IContactServiceClient
{
    Task<IEnumerable<ContactSummary>> GetContactsByLocationAsync(string location, CancellationToken cancellationToken);
    Task<List<LocationStatistic>> GetAllLocationStatisticsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ContactSummary>> GetAllContactsAsync(CancellationToken cancellationToken = default);
}