using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Providers
{
    public class SmsProvider : INotificationProvider
    {
        private readonly SmsProviderSettings _settings;
        private readonly ILogger<SmsProvider> _logger;
        private readonly HttpClient _httpClient;
        private bool _isHealthy = true;

        public ProviderType ProviderType => ProviderType.Sms;
        public NotificationPriority Priority => NotificationPriority.Normal;
        public bool IsEnabled => _settings.IsEnabled;
        public bool IsHealthy => _isHealthy;

        public SmsProvider(
            IOptions<SmsProviderSettings> options,
            ILogger<SmsProvider> logger,
            HttpClient httpClient)
        {
            _settings = options.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<INotificationResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(notification.RecipientPhoneNumber))
            {
                return new NotificationResult
                {
                    Success = false,
                    ErrorMessage = "Recipient phone number is required",
                    SentAt = DateTime.UtcNow
                };
            }

            try
            {
                // This is a placeholder for actual SMS API integration
                // In a real implementation, you would call your SMS provider's API

                _logger.LogInformation("SMS sent successfully to {Recipient}", notification.RecipientPhoneNumber);

                // Simulate successful sending
                await Task.Delay(100, cancellationToken);

                return new NotificationResult
                {
                    Success = true,
                    ProviderReference = Guid.NewGuid().ToString(),
                    SentAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {Recipient}", notification.RecipientPhoneNumber);

                return new NotificationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        public Task<bool> ValidateAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(notification.RecipientPhoneNumber))
            {
                return Task.FromResult(false);
            }

            // Basic phone number validation (simple check for now)
            var phoneNumber = notification.RecipientPhoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "");
            var isValid = phoneNumber.Length >= 10 && phoneNumber.All(char.IsDigit);

            return Task.FromResult(isValid);
        }

        public async Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // This is a placeholder for actual SMS API health check
                // In a real implementation, you would call your SMS provider's health or status endpoint

                // Simulate API call
                await Task.Delay(100, cancellationToken);
                
                var responseTime = DateTime.UtcNow - startTime;
                _isHealthy = true;

                return new ProviderHealthStatus
                {
                    IsHealthy = true,
                    Status = "Connected",
                    ResponseTime = responseTime
                };
            }
            catch (Exception ex)
            {
                var responseTime = DateTime.UtcNow - startTime;
                _isHealthy = false;

                _logger.LogError(ex, "SMS provider health check failed");
                
                return new ProviderHealthStatus
                {
                    IsHealthy = false,
                    Status = "Failed",
                    ResponseTime = responseTime,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<Dictionary<string, object>> GetConfigurationAsync()
        {
            return Task.FromResult(new Dictionary<string, object>
            {
                { "Provider", ProviderType.ToString() },
                { "Priority", Priority.ToString() },
                { "IsEnabled", IsEnabled },
                { "IsHealthy", IsHealthy },
                { "ApiUrl", _settings.ApiUrl },
                { "DefaultSender", _settings.DefaultSender }
            });
        }
    }
}
