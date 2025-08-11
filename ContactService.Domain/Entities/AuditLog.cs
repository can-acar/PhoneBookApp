using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ContactService.Domain.Entities;

/// <summary>
/// Audit log entity for MongoDB storage
/// Tracks all data changes and user actions for compliance and security
/// </summary>
public class AuditLog
{
    public AuditLog()
    {
        Id = ObjectId.GenerateNewId().ToString();
        AuditId = Guid.NewGuid().ToString();
        Timestamp = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    public AuditLog(
        string correlationId,
        string serviceName,
        string action,
        string entityType,
        string entityId,
        string? userId = null,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null) : this()
    {
        CorrelationId = correlationId;
        ServiceName = serviceName;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        UserId = userId;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// MongoDB document ID
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Unique audit record identifier
    /// </summary>
    [BsonElement("id")]
    public string AuditId { get; set; }

    /// <summary>
    /// Correlation ID for request tracing across services
    /// </summary>
    [BsonElement("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the service that generated this audit log
    /// </summary>
    [BsonElement("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// User ID who performed the action (if available)
    /// </summary>
    [BsonElement("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Action performed (CREATE, UPDATE, DELETE, etc.)
    /// </summary>
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected (Contact, ContactInfo, etc.)
    /// </summary>
    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity
    /// </summary>
    [BsonElement("entityId")]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Previous values before the change (JSON)
    /// </summary>
    [BsonElement("oldValues")]
    public object? OldValues { get; set; }

    /// <summary>
    /// New values after the change (JSON)
    /// </summary>
    [BsonElement("newValues")]
    public object? NewValues { get; set; }

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// IP address of the client
    /// </summary>
    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata about the request
    /// </summary>
    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; }

    // Helper methods
    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public void SetRequestMetadata(string requestPath, string httpMethod, int statusCode)
    {
        Metadata["requestPath"] = requestPath;
        Metadata["httpMethod"] = httpMethod;
        Metadata["statusCode"] = statusCode;
    }

    public void SetPerformanceMetadata(double durationMs, long? memoryUsed = null)
    {
        Metadata["durationMs"] = durationMs;
        if (memoryUsed.HasValue)
        {
            Metadata["memoryUsed"] = memoryUsed.Value;
        }
    }

    public void SetGraphQLMetadata(string? operationName, int? complexity = null, int? depth = null)
    {
        if (!string.IsNullOrEmpty(operationName))
        {
            Metadata["graphqlOperation"] = operationName;
        }
        if (complexity.HasValue)
        {
            Metadata["graphqlComplexity"] = complexity.Value;
        }
        if (depth.HasValue)
        {
            Metadata["graphqlDepth"] = depth.Value;
        }
    }
}