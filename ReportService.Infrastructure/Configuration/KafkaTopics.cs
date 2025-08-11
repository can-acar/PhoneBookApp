namespace ReportService.Infrastructure.Configuration;

public class KafkaTopics
{
    public string ReportRequests { get; set; } = "report-requests";
    public string ReportCompleted { get; set; } = "report-completed";
    // Backward compatibility (legacy services)
    public string ReportEvents { get; set; } = "report-events";
    public string NotificationEvents { get; set; } = "notification-events";
}