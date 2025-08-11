namespace ReportService.Domain.Models.Kafka;

public class ReportCompletedMessage
{
    public Guid ReportId { get; set; }
    public DateTime CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
    public int PersonCount { get; set; }
    public int PhoneNumberCount { get; set; }
}