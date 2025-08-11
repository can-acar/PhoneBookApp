using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Domain.Interfaces
{
    public interface INotificationProviderManager
    {
        Task<INotificationResult> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
        Task<Dictionary<ProviderType, ProviderHealthStatus>> CheckAllProvidersHealthAsync(CancellationToken cancellationToken = default);
        INotificationProvider GetProvider(ProviderType providerType);
        IEnumerable<INotificationProvider> GetAllProviders();
        IEnumerable<INotificationProvider> GetActiveProviders();
    }
}
