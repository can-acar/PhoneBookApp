using System.ComponentModel.DataAnnotations;

namespace ContactService.Infrastructure.Configuration;

public class KafkaSettings
{
    public const string SectionName = "Kafka";
    
    [Required]
    public string BootstrapServers { get; set; } = string.Empty;
    
    [Required]
    public string GroupId { get; set; } = string.Empty;
    
    [Required]
    public string ClientId { get; set; } = string.Empty;
    
    public KafkaTopics Topics { get; set; } = new();
    public KafkaProducerConfig ProducerConfig { get; set; } = new();
}

public class KafkaTopics
{
    [Required]
    public string ContactEvents { get; set; } = string.Empty;
    
    [Required]
    public string ReportEvents { get; set; } = string.Empty;
    
    [Required]
    public string NotificationEvents { get; set; } = string.Empty;
}

public class KafkaProducerConfig
{
    public string Acks { get; set; } = "all";
    public int MessageTimeoutMs { get; set; } = 30000;
    public int RequestTimeoutMs { get; set; } = 30000;
    public bool EnableIdempotence { get; set; } = true;
    public int Retries { get; set; } = 3;
    public int RetryBackoffMs { get; set; } = 100;
    public int BatchSize { get; set; } = 16384;
    public int LingerMs { get; set; } = 10;
    public string CompressionType { get; set; } = "snappy";
}