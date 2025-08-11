using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.ApplicationService.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly ILogger<TemplateService> _logger;

        public TemplateService(
            INotificationTemplateRepository templateRepository,
            ILogger<TemplateService> logger)
        {
            _templateRepository = templateRepository;
            _logger = logger;
        }

        public async Task<bool> TemplateExistsAsync(string templateName, string? language = null)
        {
            var template = await _templateRepository.GetByNameAsync(templateName, language);
            return template != null;
        }

        public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, object> templateData, string? language = null)
        {
            var template = await _templateRepository.GetByNameAsync(templateName, language);
            if (template == null)
            {
                _logger.LogWarning("Template not found: {TemplateName}, Language: {Language}", templateName, language ?? "Default");
                throw new ArgumentException($"Template not found: {templateName}");
            }

            var content = template.ContentTemplate;
            
            // Simple placeholder replacement
            foreach (var item in templateData)
            {
                content = content.Replace($"{{{item.Key}}}", item.Value?.ToString() ?? string.Empty);
            }

            return content;
        }

        public async Task<(string subject, string content)> RenderNotificationTemplateAsync(
            string templateName, Dictionary<string, object> templateData, string? language = null)
        {
            var template = await _templateRepository.GetByNameAsync(templateName, language);
            if (template == null)
            {
                _logger.LogWarning("Template not found: {TemplateName}, Language: {Language}", templateName, language ?? "Default");
                throw new ArgumentException($"Template not found: {templateName}");
            }

            var subject = template.SubjectTemplate;
            var content = template.ContentTemplate;
            
            // Simple placeholder replacement
            foreach (var item in templateData)
            {
                subject = subject.Replace($"{{{item.Key}}}", item.Value?.ToString() ?? string.Empty);
                content = content.Replace($"{{{item.Key}}}", item.Value?.ToString() ?? string.Empty);
            }

            return (subject, content);
        }
    }
}
