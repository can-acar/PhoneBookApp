using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.ApplicationService.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationProviderManager _providerManager;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            INotificationProviderManager providerManager,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _providerManager = providerManager;
            _logger = logger;
        }

        public async Task<Guid> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            await _notificationRepository.CreateAsync(notification);
            return notification.Id;
        }

        public async Task<INotificationResult> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending notification: {NotificationId}, Type: {ProviderType}", 
                notification.Id, notification.PreferredProvider);

            try
            {
                var result = await _providerManager.SendNotificationAsync(notification, cancellationToken);

                // Update notification status
                if (result.Success)
                {
                    notification.MarkAsSent(notification.PreferredProvider);
                }
                else
                {
                    notification.MarkAsFailed(result.ErrorMessage ?? "Unknown error");
                }

                await _notificationRepository.UpdateAsync(notification);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {NotificationId}", notification.Id);
                
                notification.MarkAsFailed($"Exception: {ex.Message}");
                await _notificationRepository.UpdateAsync(notification);
                
                return new NotificationResult 
                { 
                    Success = false, 
                    ErrorMessage = ex.Message,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting notifications for user: {UserId}", userId);
            return await _notificationRepository.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Notification>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting notifications by correlation ID: {CorrelationId}", correlationId);
            return await _notificationRepository.GetByCorrelationIdAsync(correlationId);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _notificationRepository.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Notification>> GetByCorrelationIdAsync(string correlationId)
        {
            return await _notificationRepository.GetByCorrelationIdAsync(correlationId);
        }
    }

 
}
