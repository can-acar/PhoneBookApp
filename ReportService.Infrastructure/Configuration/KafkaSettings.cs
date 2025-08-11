namespace ReportService.Infrastructure.Configuration
{
    public class KafkaSettings
    {
    public string BootstrapServers { get; set; } = "kafka:29092";
        public string ClientId { get; set; } = "report-service";

        public string GroupId { get; set; } = "report-service-group";

        // Preferred nested topics object
        public KafkaTopics Topics { get; set; } = new();

        // Fallback flat keys to support env like Kafka__ReportRequestsTopic
        // If provided, these will be copied into Topics via PostConfigure
        public string? ReportRequestsTopic { get; set; }
        public string? ReportCompletedTopic { get; set; }
    }
}