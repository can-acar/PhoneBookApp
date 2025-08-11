namespace ReportService.Domain.Models.Kafka
{
    /// <summary>
    /// Message model for Kafka report generation requests
    /// Clean Architecture: Domain model for inter-service communication
    /// </summary>
    public class ReportRequestMessage
    {
        public Guid ReportId { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Location { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }
}
