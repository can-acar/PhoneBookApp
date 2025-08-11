using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Domain.Entities
{
    public class NotificationResult : INotificationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ProviderReference { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
