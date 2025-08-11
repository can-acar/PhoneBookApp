using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.CrossCutting.CorrelationId;
using MongoDB.Bson;

namespace ContactService.Infrastructure.Services;

/// <summary>
/// Service implementation for audit logging operations
/// Provides high-level audit trail functionality with business logic
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<AuditLogService> _logger;
    private const string ServiceName = "ContactService";

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        ICorrelationIdProvider correlationIdProvider,
        ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _correlationIdProvider = correlationIdProvider;
        _logger = logger;
    }

    public async Task LogCreateAsync<T>(
        string correlationId,
        string entityId,
        T newEntity,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var auditLog = new AuditLog(
            correlationId,
            ServiceName,
            "CREATE",
            typeof(T).Name,
            entityId,
            userId,
            oldValues: null,
            newValues: SerializeEntity(newEntity),
            ipAddress,
            userAgent);

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                auditLog.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Audit log created for CREATE action on {EntityType}:{EntityId} by user {UserId}",
            typeof(T).Name, entityId, userId ?? "system");
    }

    public async Task LogUpdateAsync<T>(
        string correlationId,
        string entityId,
        T? oldEntity,
        T newEntity,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var auditLog = new AuditLog(
            correlationId,
            ServiceName,
            "UPDATE",
            typeof(T).Name,
            entityId,
            userId,
            oldValues: oldEntity != null ? SerializeEntity(oldEntity) : null,
            newValues: SerializeEntity(newEntity),
            ipAddress,
            userAgent);

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                auditLog.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        // Add change detection metadata
        if (oldEntity != null)
        {
            var changes = DetectChanges(oldEntity, newEntity);
            if (changes.Any())
            {
                auditLog.AddMetadata("changedFields", changes.Keys.ToArray());
                auditLog.AddMetadata("changeCount", changes.Count);
            }
        }

        await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Audit log created for UPDATE action on {EntityType}:{EntityId} by user {UserId}",
            typeof(T).Name, entityId, userId ?? "system");
    }

    public async Task LogDeleteAsync<T>(
        string correlationId,
        string entityId,
        T? deletedEntity,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var auditLog = new AuditLog(
            correlationId,
            ServiceName,
            "DELETE",
            typeof(T).Name,
            entityId,
            userId,
            oldValues: deletedEntity != null ? SerializeEntity(deletedEntity) : null,
            newValues: null,
            ipAddress,
            userAgent);

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                auditLog.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Audit log created for DELETE action on {EntityType}:{EntityId} by user {UserId}",
            typeof(T).Name, entityId, userId ?? "system");
    }

    public async Task LogActionAsync(
        string correlationId,
        string action,
        string entityType,
        string entityId,
        object? data = null,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(
            correlationId,
            ServiceName,
            action.ToUpperInvariant(),
            entityType,
            entityId,
            userId,
            oldValues: null,
            newValues: data != null ? SerializeEntity(data) : null,
            ipAddress,
            userAgent);

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                auditLog.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Audit log created for {Action} action on {EntityType}:{EntityId} by user {UserId}",
            action, entityType, entityId, userId ?? "system");
    }

    public async Task LogGraphQLOperationAsync(
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
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(
            correlationId,
            ServiceName,
            $"GRAPHQL_{operationType.ToUpperInvariant()}",
            "GraphQLOperation",
            operationName,
            userId,
            oldValues: null,
            newValues: variables != null ? SerializeEntity(variables) : null,
            ipAddress,
            userAgent);

        auditLog.SetGraphQLMetadata(operationName, complexity, depth);
        
        if (durationMs.HasValue)
        {
            auditLog.SetPerformanceMetadata(durationMs.Value);
        }

        await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

        _logger.LogDebug(
            "Audit log created for GraphQL {OperationType} operation {OperationName} by user {UserId}",
            operationType, operationName, userId ?? "anonymous");
    }

    public async Task LogAuthenticationAsync(
        string correlationId,
        string action,
        string? userId = null,
        bool success = false,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(
            correlationId,
            ServiceName,
            $"AUTH_{action.ToUpperInvariant()}",
            "Authentication",
            userId ?? "anonymous",
            userId,
            oldValues: null,
            newValues: new
            {
                success = success,
                reason = reason,
                timestamp = DateTime.UtcNow
            },
            ipAddress,
            userAgent);

        auditLog.AddMetadata("authSuccess", success);
        if (!string.IsNullOrEmpty(reason))
        {
            auditLog.AddMetadata("authReason", reason);
        }

        await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Authentication audit log created for {Action} by user {UserId}: {Success}",
            action, userId ?? "anonymous", success ? "Success" : "Failed");
    }

    public async Task LogAuthorizationAsync(
        string correlationId,
        string resource,
        string action,
        string? userId = null,
        bool authorized = false,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(
            correlationId,
            ServiceName,
            $"AUTHZ_{action.ToUpperInvariant()}",
            "Authorization",
            resource,
            userId,
            oldValues: null,
            newValues: new
            {
                resource = resource,
                action = action,
                authorized = authorized,
                reason = reason,
                timestamp = DateTime.UtcNow
            },
            ipAddress,
            userAgent);

        auditLog.AddMetadata("authzAuthorized", authorized);
        auditLog.AddMetadata("authzResource", resource);
        if (!string.IsNullOrEmpty(reason))
        {
            auditLog.AddMetadata("authzReason", reason);
        }

        await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Authorization audit log created for {Action} on {Resource} by user {UserId}: {Authorized}",
            action, resource, userId ?? "anonymous", authorized ? "Authorized" : "Denied");
    }

    public async Task LogSystemEventAsync(
        string correlationId,
        string eventType,
        string description,
        object? data = null,
        string? severity = "Information",
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(
            correlationId,
            ServiceName,
            $"SYSTEM_{eventType.ToUpperInvariant()}",
            "SystemEvent",
            eventType,
            userId: null,
            oldValues: null,
            newValues: new
            {
                eventType = eventType,
                description = description,
                severity = severity,
                data = data,
                timestamp = DateTime.UtcNow
            });

    auditLog.AddMetadata("eventSeverity", severity ?? "Information");
        auditLog.AddMetadata("eventDescription", description);

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                auditLog.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "System event audit log created for {EventType}: {Description}",
            eventType, description);
    }

    public async Task<IEnumerable<AuditLog>> GetEntityAuditTrailAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        return await _auditLogRepository.GetByEntityAsync(entityType, entityId, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetUserActivityAsync(
        string userId,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _auditLogRepository.SearchAsync(
            userId: userId,
            startTime: startTime,
            endTime: endTime,
            skip: skip,
            take: take,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetRequestAuditTrailAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await _auditLogRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
    }

    public async Task<AuditStatistics> GetAuditStatisticsAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        var actionStats = await _auditLogRepository.GetActionStatisticsAsync(startTime, endTime, cancellationToken);
        var serviceStats = await _auditLogRepository.GetServiceStatisticsAsync(startTime, endTime, cancellationToken);
        var totalCount = await _auditLogRepository.GetTotalCountAsync(cancellationToken);

        var statistics = new AuditStatistics
        {
            TotalAuditLogs = totalCount,
            ActionCounts = actionStats,
            ServiceCounts = serviceStats,
            EntityTypeCounts = new Dictionary<string, long>(),
            AverageLogsPerDay = CalculateAverageLogsPerDay(totalCount, startTime, endTime)
        };

        return statistics;
    }

    private static object SerializeEntity(object entity)
    {
        try
        {
            // Use anonymous object to avoid circular references and sensitive data
            var serialized = JsonConvert.SerializeObject(entity, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            // Return a BsonDocument so Mongo driver can serialize it without needing Newtonsoft types
            return BsonDocument.Parse(serialized);
        }
        catch (Exception)
        {
            // Fallback to simple representation
            return new BsonDocument
            {
                { "type", entity.GetType().Name },
                { "toString", entity?.ToString() ?? string.Empty }
            };
        }
    }

    private static Dictionary<string, object> DetectChanges<T>(T oldEntity, T newEntity) where T : class
    {
        var changes = new Dictionary<string, object>();

        try
        {
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (!property.CanRead) continue;

                var oldValue = property.GetValue(oldEntity);
                var newValue = property.GetValue(newEntity);

                if (!Equals(oldValue, newValue))
                {
                    changes[property.Name] = new
                    {
                        from = oldValue,
                        to = newValue
                    };
                }
            }
        }
        catch (Exception)
        {
            // Fallback - at least indicate that changes were detected
            changes["_changeDetectionFailed"] = true;
        }

        return changes;
    }

    private static double CalculateAverageLogsPerDay(long totalLogs, DateTime? startTime, DateTime? endTime)
    {
        var start = startTime ?? DateTime.UtcNow.AddDays(-30);
        var end = endTime ?? DateTime.UtcNow;
        var days = Math.Max((end - start).TotalDays, 1);

        return totalLogs / days;
    }
}