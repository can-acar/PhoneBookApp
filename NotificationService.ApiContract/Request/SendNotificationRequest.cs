using NotificationService.Domain.Enums;

namespace NotificationService.ApiContract.Request
{
    public class SendNotificationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? RecipientEmail { get; set; }
        public string? RecipientPhoneNumber { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public ProviderType PreferredProvider { get; set; } = ProviderType.Email;
        public Dictionary<string, string>? AdditionalData { get; set; }
        public string? CorrelationId { get; set; }
    }
}
