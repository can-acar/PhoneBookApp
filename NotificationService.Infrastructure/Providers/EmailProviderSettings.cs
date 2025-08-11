namespace NotificationService.Infrastructure.Providers;

public class EmailProviderSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DefaultFromAddress { get; set; } = string.Empty;
    public string DefaultFromName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}