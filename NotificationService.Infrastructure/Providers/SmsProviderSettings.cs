namespace NotificationService.Infrastructure.Providers;

public class SmsProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string DefaultSender { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}