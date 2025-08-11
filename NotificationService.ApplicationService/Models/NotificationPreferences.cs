namespace NotificationService.ApplicationService.Models;

public class NotificationPreferences
{
    public bool EnableEmail { get; set; } = true;
    public bool EnableSms { get; set; } = false;
    public bool EnableWebSocket { get; set; } = false;
    public string Language { get; set; } = "tr-TR";
}