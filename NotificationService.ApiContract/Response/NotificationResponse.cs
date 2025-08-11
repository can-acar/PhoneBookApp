using NotificationService.Domain.Enums;

namespace NotificationService.ApiContract.Response
{
    public class NotificationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? RecipientEmail { get; set; }
        public string? RecipientPhoneNumber { get; set; }
        public NotificationPriority Priority { get; set; }
        public ProviderType PreferredProvider { get; set; }
        public Dictionary<string, string>? AdditionalData { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }
}
