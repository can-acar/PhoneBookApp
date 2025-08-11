using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces
{
    public interface INotificationService
    {
        Task<Guid> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
        Task<INotificationResult> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Notification>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    }
}
