using System.Text.Json.Serialization;
using ReportService.Domain.Enums;

namespace ReportService.Domain.Events;

public class ReportCompletedEvent
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "ReportCompleted";
    
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
    
    [JsonPropertyName("status")]
    public ReportStatus Status { get; set; }
    
    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }
    
    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; }
    
    [JsonPropertyName("processingTimeSeconds")]
    public double ProcessingTimeSeconds { get; set; }
    
    [JsonPropertyName("summary")]
    public ReportSummary Summary { get; set; } = new();
    
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ReportSummary
{
    [JsonPropertyName("totalPersons")]
    public int TotalPersons { get; set; }
    
    [JsonPropertyName("totalPhoneNumbers")]
    public int TotalPhoneNumbers { get; set; }
    
    [JsonPropertyName("locationCount")]
    public int LocationCount { get; set; }
    
    [JsonPropertyName("fileSize")]
    public string? FileSize { get; set; }
    
    [JsonPropertyName("format")]
    public string? Format { get; set; }
}
