using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces
{
    public interface INotificationTemplateRepository
    {
        Task<NotificationTemplate?> GetByNameAsync(string templateName, string? language = null);
        Task<IEnumerable<NotificationTemplate>> GetAllActiveAsync();
        Task CreateAsync(NotificationTemplate template);
        Task UpdateAsync(NotificationTemplate template);
        Task<bool> DeleteAsync(Guid id);
    }
}
