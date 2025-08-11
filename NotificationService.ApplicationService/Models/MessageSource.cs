namespace NotificationService.ApplicationService.Models;

public class MessageSource
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceVersion { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}