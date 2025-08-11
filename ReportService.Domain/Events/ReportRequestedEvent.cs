using System.Text.Json.Serialization;

namespace ReportService.Domain.Events;

public class ReportRequestedEvent
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "ReportRequested";
    
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("reportId")]
    public Guid ReportId { get; set; }
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
    
    [JsonPropertyName("requestedBy")]
    public string RequestedBy { get; set; } = string.Empty;
    
    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}
