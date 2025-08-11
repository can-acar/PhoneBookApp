using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification> GetByIdAsync(Guid id);
        Task<IEnumerable<Notification>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Notification>> GetUndeliveredAsync();
        Task CreateAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task<IEnumerable<Notification>> GetByCorrelationIdAsync(string correlationId);
    }
}
