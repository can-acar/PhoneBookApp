namespace NotificationService.ApplicationService.Models;

public class ReportCompletedEvent
{
    public string RequestId { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int GenerationTimeSeconds { get; set; }
    public ReportSummary? Summary { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime DownloadExpiresAt { get; set; }
    public string FileSize { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public NotificationPreferences? NotificationPreferences { get; set; }
    public string? UserEmail { get; set; }
    public string? UserPhoneNumber { get; set; }
}