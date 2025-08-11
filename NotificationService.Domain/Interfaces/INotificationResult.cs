namespace NotificationService.Domain.Interfaces;

public interface INotificationResult
{
    bool Success { get; set; }
    string? ErrorMessage { get; set; }
    string? ProviderReference { get; set; }
    DateTime? SentAt { get; set; }
}