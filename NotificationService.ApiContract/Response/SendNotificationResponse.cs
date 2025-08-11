using System;

namespace NotificationService.ApiContract.Response
{
    public class SendNotificationResponse
    {
        public string Id { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
