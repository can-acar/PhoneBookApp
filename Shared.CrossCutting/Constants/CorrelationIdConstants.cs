namespace Shared.CrossCutting.Constants;

/// <summary>
/// Constants related to correlation ID handling
/// </summary>
public static class CorrelationIdConstants
{
    /// <summary>
    /// The HTTP header name for correlation ID
    /// </summary>
    public const string CORRELATION_ID_HEADER = "X-Correlation-ID";
    
    /// <summary>
    /// The HTTP context item key for correlation ID
    /// </summary>
    public const string CORRELATION_ID_CONTEXT_KEY = "CorrelationId";
    
    /// <summary>
    /// The logging scope key for correlation ID
    /// </summary>
    public const string CORRELATION_ID_LOG_KEY = "CorrelationId";
    
    /// <summary>
    /// The gRPC metadata key for correlation ID
    /// </summary>
    public const string GRPC_CORRELATION_ID_KEY = "x-correlation-id";
    
    /// <summary>
    /// The Kafka header key for correlation ID
    /// </summary>
    public const string KAFKA_CORRELATION_ID_KEY = "CorrelationId";
}