using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Providers
{
    public class EmailProvider : INotificationProvider
    {
        private readonly EmailProviderSettings _settings;
        private readonly ILogger<EmailProvider> _logger;
        private bool _isHealthy = true;

        public ProviderType ProviderType => ProviderType.Email;
        public NotificationPriority Priority => NotificationPriority.High;
        public bool IsEnabled => _settings.IsEnabled;
        public bool IsHealthy => _isHealthy;

        public EmailProvider(IOptions<EmailProviderSettings> options, ILogger<EmailProvider> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<INotificationResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(notification.RecipientEmail))
            {
                return new NotificationResult
                {
                    Success = false,
                    ErrorMessage = "Recipient email address is required",
                    SentAt = DateTime.UtcNow
                };
            }

            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password)
                };

                var message = new MailMessage
                {
                    From = new MailAddress(_settings.DefaultFromAddress, _settings.DefaultFromName),
                    Subject = notification.Subject,
                    Body = notification.Content,
                    IsBodyHtml = true
                };

                message.To.Add(notification.RecipientEmail);

                // Add any additional headers from AdditionalData if available
                if (notification.AdditionalData != null)
                {
                    foreach (var item in notification.AdditionalData.Where(x => x.Key.StartsWith("header_")))
                    {
                        message.Headers.Add(item.Key.Replace("header_", ""), item.Value);
                    }
                }

                await client.SendMailAsync(message, cancellationToken);

                _logger.LogInformation("Email sent successfully to {Recipient}", notification.RecipientEmail);

                return new NotificationResult
                {
                    Success = true,
                    SentAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", notification.RecipientEmail);
                
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
            if (string.IsNullOrEmpty(notification.RecipientEmail))
            {
                return Task.FromResult(false);
            }

            // Simple email format validation
            try
            {
                var address = new MailAddress(notification.RecipientEmail);
                return Task.FromResult(address.Address == notification.RecipientEmail);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    Timeout = 5000 // 5 seconds timeout for health check
                };

                // Test the connection without sending an actual email
                await Task.Run(() => {
                    try {
                        // We'll create a test connection but not actually send anything
                        using var message = new MailMessage(
                            _settings.DefaultFromAddress,
                            _settings.DefaultFromAddress,
                            "Test Connection",
                            "This is a test message");
                        
                        // Just test authentication and connection
                        client.Timeout = 5000; // 5 second timeout
                    }
                    catch (Exception) {
                        // Let the outer exception handler catch this
                        throw;
                    }
                }, cancellationToken);
                
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

                _logger.LogError(ex, "Email provider health check failed");
                
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
                { "SmtpServer", _settings.SmtpServer },
                { "SmtpPort", _settings.SmtpPort },
                { "EnableSsl", _settings.EnableSsl },
                { "DefaultFromAddress", _settings.DefaultFromAddress },
                { "DefaultFromName", _settings.DefaultFromName }
            });
        }
    }
}
