using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Interfaces
{
    public interface INotificationProvider
    {
        ProviderType ProviderType { get; }
        NotificationPriority Priority { get; }
        bool IsEnabled { get; }
        bool IsHealthy { get; }

        Task<INotificationResult> SendAsync(Notification notification, CancellationToken cancellationToken = default);
        Task<bool> ValidateAsync(Notification notification, CancellationToken cancellationToken = default);
        Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<string, object>> GetConfigurationAsync();
    }
}
