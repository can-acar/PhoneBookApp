namespace NotificationService.ApplicationService.Models;

public class KafkaMessage<T>
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.0";
    public MessageSource Source { get; set; } = new();
    public T? Data { get; set; }

    public KafkaMessage()
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }
}