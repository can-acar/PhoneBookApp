using ContactService.Domain.Entities;

namespace ContactService.Domain.Interfaces;

/// <summary>
/// Repository interface for audit log operations in MongoDB
/// Provides methods for storing and querying audit trail data
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Create a single audit log entry
    /// </summary>
    Task<AuditLog> CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk insert multiple audit log entries for performance
    /// </summary>
    Task CreateManyAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by correlation ID for request tracing
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by user ID
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by service name
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs within a time range
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search audit logs with multiple criteria
    /// </summary>
    Task<IEnumerable<AuditLog>> SearchAsync(
        string? correlationId = null,
        string? serviceName = null,
        string? userId = null,
        string? action = null,
        string? entityType = null,
        string? entityId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit log statistics for monitoring
    /// </summary>
    Task<Dictionary<string, long>> GetStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get most frequent actions for analysis
    /// </summary>
    Task<Dictionary<string, long>> GetActionStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs count by service
    /// </summary>
    Task<Dictionary<string, long>> GetServiceStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old audit logs for data retention (admin only)
    /// </summary>
    Task<long> DeleteOldLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total audit logs count
    /// </summary>
    Task<long> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if audit logging is healthy (connection test)
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}