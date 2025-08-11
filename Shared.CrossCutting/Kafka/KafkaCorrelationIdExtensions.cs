using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Shared.CrossCutting.Constants;
using Shared.CrossCutting.CorrelationId;

namespace Shared.CrossCutting.Kafka;

/// <summary>
/// Kafka message extensions for correlation ID handling
/// </summary>
public static class KafkaCorrelationIdExtensions
{
    /// <summary>
    /// Adds correlation ID to Kafka message headers
    /// </summary>
    public static void AddCorrelationId(this Headers headers, string correlationId, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

        headers.Add(CorrelationIdConstants.KAFKA_CORRELATION_ID_KEY, Encoding.UTF8.GetBytes(correlationId));
        headers.Add("Source", Encoding.UTF8.GetBytes(serviceName));
        headers.Add("Timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")));
    }

    /// <summary>
    /// Extracts correlation ID from Kafka message headers
    /// </summary>
    public static string? ExtractCorrelationId(this Headers headers)
    {
        if (headers.TryGetLastBytes(CorrelationIdConstants.KAFKA_CORRELATION_ID_KEY, out var correlationIdBytes))
        {
            return Encoding.UTF8.GetString(correlationIdBytes);
        }
        return null;
    }

    /// <summary>
    /// Creates a Kafka message with correlation ID
    /// </summary>
    public static Message<string, string> CreateMessageWithCorrelationId(
        string correlationId, 
        string serviceName, 
        string messageContent)
    {
        var headers = new Headers();
        headers.AddCorrelationId(correlationId, serviceName);

        return new Message<string, string>
        {
            Key = correlationId,
            Value = messageContent,
            Headers = headers
        };
    }

    /// <summary>
    /// Creates a logging scope with correlation ID for Kafka operations
    /// </summary>
    public static IDisposable CreateKafkaLoggingScope(
        this ILogger logger, 
        string correlationId, 
        string topic, 
        string operation)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdConstants.CORRELATION_ID_LOG_KEY] = correlationId,
            ["KafkaTopic"] = topic,
            ["KafkaOperation"] = operation
        });
    }
}
