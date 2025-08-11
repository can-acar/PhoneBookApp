using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;

namespace NotificationService.ApplicationService.Services
{
    public class NotificationProviderManager : INotificationProviderManager
    {
        private readonly IEnumerable<INotificationProvider> _providers;
        private readonly ILogger<NotificationProviderManager> _logger;

        public NotificationProviderManager(
            IEnumerable<INotificationProvider> providers,
            ILogger<NotificationProviderManager> logger)
        {
            _providers = providers;
            _logger = logger;
        }

        public async Task<Dictionary<ProviderType, ProviderHealthStatus>> CheckAllProvidersHealthAsync(CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<ProviderType, ProviderHealthStatus>();

            foreach (var provider in _providers)
            {
                try
                {
                    var status = await provider.CheckHealthAsync(cancellationToken);
                    results[provider.ProviderType] = status;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking health of provider {ProviderType}", provider.ProviderType);
                    
                    results[provider.ProviderType] = new ProviderHealthStatus
                    {
                        IsHealthy = false,
                        Status = "Error",
                        ErrorMessage = ex.Message,
                        ResponseTime = TimeSpan.Zero
                    };
                }
            }

            return results;
        }

        public IEnumerable<INotificationProvider> GetActiveProviders()
        {
            return _providers.Where(p => p.IsEnabled && p.IsHealthy);
        }

        public IEnumerable<INotificationProvider> GetAllProviders()
        {
            return _providers;
        }

        public INotificationProvider GetProvider(ProviderType providerType)
        {
            return _providers.FirstOrDefault(p => p.ProviderType == providerType) 
                ?? throw new ArgumentException($"Provider not found for type: {providerType}");
        }

        public async Task<INotificationResult> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            // Get the preferred provider
            var provider = _providers.FirstOrDefault(p => p.ProviderType == notification.PreferredProvider && p.IsEnabled);

            // If preferred provider is not available, find an alternative based on priority
            if (provider == null)
            {
                _logger.LogWarning("Preferred provider {ProviderType} not available. Finding alternative.", 
                    notification.PreferredProvider);
                
                provider = _providers.Where(p => p.IsEnabled && p.IsHealthy)
                    .OrderByDescending(p => p.Priority)
                    .FirstOrDefault();
            }

            if (provider == null)
            {
                _logger.LogError("No suitable provider found for notification {NotificationId}", notification.Id);
                
                return new NotificationResult
                {
                    Success = false,
                    ErrorMessage = "No suitable notification provider available",
                    SentAt = DateTime.UtcNow
                };
            }

            // Validate notification for the selected provider
            var isValid = await provider.ValidateAsync(notification, cancellationToken);
            if (!isValid)
            {
                _logger.LogError("Notification {NotificationId} validation failed for provider {ProviderType}", 
                    notification.Id, provider.ProviderType);
                
                return new NotificationResult
                {
                    Success = false,
                    ErrorMessage = $"Notification validation failed for provider {provider.ProviderType}",
                    SentAt = DateTime.UtcNow
                };
            }

            // Send the notification
            _logger.LogInformation("Sending notification {NotificationId} using provider {ProviderType}", 
                notification.Id, provider.ProviderType);
            
            return await provider.SendAsync(notification, cancellationToken);
        }
    }
}
