using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces
{
    public interface ITemplateService
    {
        Task<string> RenderTemplateAsync(string templateName, Dictionary<string, object> templateData, string? language = null);
        Task<(string subject, string content)> RenderNotificationTemplateAsync(string templateName, Dictionary<string, object> templateData, string? language = null);
        Task<bool> TemplateExistsAsync(string templateName, string? language = null);
    }
}
