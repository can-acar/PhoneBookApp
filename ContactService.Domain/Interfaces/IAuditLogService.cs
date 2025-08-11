using ContactService.Domain.Entities;

namespace ContactService.Domain.Interfaces;

/// <summary>
/// Service interface for audit logging operations
/// Provides high-level audit trail functionality with business logic
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Log a create operation
    /// </summary>
    Task LogCreateAsync<T>(
        string correlationId,
        string entityId,
        T newEntity,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Log an update operation
    /// </summary>
    Task LogUpdateAsync<T>(
        string correlationId,
        string entityId,
        T? oldEntity,
        T newEntity,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Log a delete operation
    /// </summary>
    Task LogDeleteAsync<T>(
        string correlationId,
        string entityId,
        T? deletedEntity,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Log a custom action
    /// </summary>
    Task LogActionAsync(
        string correlationId,
        string action,
        string entityType,
        string entityId,
        object? data = null,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a GraphQL operation
    /// </summary>
    Task LogGraphQLOperationAsync(
        string correlationId,
        string operationName,
        string operationType,
        object? variables = null,
        int? complexity = null,
        int? depth = null,
        double? durationMs = null,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log an authentication event
    /// </summary>
    Task LogAuthenticationAsync(
        string correlationId,
        string action,
        string? userId = null,
        bool success = false,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log an authorization event
    /// </summary>
    Task LogAuthorizationAsync(
        string correlationId,
        string resource,
        string action,
        string? userId = null,
        bool authorized = false,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a system event
    /// </summary>
    Task LogSystemEventAsync(
        string correlationId,
        string eventType,
        string description,
        object? data = null,
        string? severity = "Information",
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit trail for an entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetEntityAuditTrailAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user activity log
    /// </summary>
    Task<IEnumerable<AuditLog>> GetUserActivityAsync(
        string userId,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get request audit trail by correlation ID
    /// </summary>
    Task<IEnumerable<AuditLog>> GetRequestAuditTrailAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit statistics for monitoring
    /// </summary>
    Task<AuditStatistics> GetAuditStatisticsAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Audit statistics model
/// </summary>
public class AuditStatistics
{
    public long TotalAuditLogs { get; set; }
    public Dictionary<string, long> ActionCounts { get; set; } = new();
    public Dictionary<string, long> ServiceCounts { get; set; } = new();
    public Dictionary<string, long> EntityTypeCounts { get; set; } = new();
    public DateTime? OldestLog { get; set; }
    public DateTime? NewestLog { get; set; }
    public double AverageLogsPerDay { get; set; }
}