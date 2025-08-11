namespace NotificationService.Infrastructure
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string ReportCompletedTopic { get; set; } = string.Empty;
        public string NotificationsTopic { get; set; } = string.Empty;
        public string ErrorTopic { get; set; } = string.Empty;
    }
}
