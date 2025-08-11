using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ContactService.Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of audit log repository
/// Provides optimized audit trail storage and retrieval operations
/// </summary>
public class MongoAuditLogRepository : IAuditLogRepository
{
    private readonly MongoDbContext _mongoContext;
    private readonly ILogger<MongoAuditLogRepository> _logger;

    public MongoAuditLogRepository(MongoDbContext mongoContext, ILogger<MongoAuditLogRepository> logger)
    {
        _mongoContext = mongoContext;
        _logger = logger;
    }

    public async Task<AuditLog> CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            await _mongoContext.AuditLogs.InsertOneAsync(auditLog, cancellationToken: cancellationToken);
            
            _logger.LogDebug(
                "Created audit log {AuditId} for action {Action} on entity {EntityType}:{EntityId}",
                auditLog.AuditId, auditLog.Action, auditLog.EntityType, auditLog.EntityId);

            return auditLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create audit log for action {Action} on entity {EntityType}:{EntityId}",
                auditLog.Action, auditLog.EntityType, auditLog.EntityId);
            throw;
        }
    }

    public async Task CreateManyAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
    {
        var auditLogsList = auditLogs.ToList();
        if (!auditLogsList.Any()) return;

        try
        {
            await _mongoContext.AuditLogs.InsertManyAsync(auditLogsList, cancellationToken: cancellationToken);
            
            _logger.LogDebug("Bulk created {Count} audit logs", auditLogsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk create {Count} audit logs", auditLogsList.Count);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.Eq(x => x.CorrelationId, correlationId);
        var sort = Builders<AuditLog>.Sort.Ascending(x => x.Timestamp);

        return await _mongoContext.AuditLogs
            .Find(filter)
            .Sort(sort)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.And(
            Builders<AuditLog>.Filter.Eq(x => x.EntityType, entityType),
            Builders<AuditLog>.Filter.Eq(x => x.EntityId, entityId));

        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);

        return await _mongoContext.AuditLogs
            .Find(filter)
            .Sort(sort)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.Eq(x => x.UserId, userId);
        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);

        return await _mongoContext.AuditLogs
            .Find(filter)
            .Sort(sort)
            .Limit(1000) // Limit to prevent large result sets
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.Eq(x => x.ServiceName, serviceName);
        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);

        return await _mongoContext.AuditLogs
            .Find(filter)
            .Sort(sort)
            .Limit(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.Eq(x => x.Action, action);
        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);

        return await _mongoContext.AuditLogs
            .Find(filter)
            .Sort(sort)
            .Limit(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.And(
            Builders<AuditLog>.Filter.Gte(x => x.Timestamp, startTime),
            Builders<AuditLog>.Filter.Lte(x => x.Timestamp, endTime));

        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);

        return await _mongoContext.AuditLogs
            .Find(filter)
            .Sort(sort)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> SearchAsync(
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
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<AuditLog>.Filter;
        var filters = new List<FilterDefinition<AuditLog>>();

        if (!string.IsNullOrEmpty(correlationId))
            filters.Add(filterBuilder.Eq(x => x.CorrelationId, correlationId));

        if (!string.IsNullOrEmpty(serviceName))
            filters.Add(filterBuilder.Eq(x => x.ServiceName, serviceName));

        if (!string.IsNullOrEmpty(userId))
            filters.Add(filterBuilder.Eq(x => x.UserId, userId));

        if (!string.IsNullOrEmpty(action))
            filters.Add(filterBuilder.Eq(x => x.Action, action));

        if (!string.IsNullOrEmpty(entityType))
            filters.Add(filterBuilder.Eq(x => x.EntityType, entityType));

        if (!string.IsNullOrEmpty(entityId))
            filters.Add(filterBuilder.Eq(x => x.EntityId, entityId));

        if (startTime.HasValue)
            filters.Add(filterBuilder.Gte(x => x.Timestamp, startTime.Value));

        if (endTime.HasValue)
            filters.Add(filterBuilder.Lte(x => x.Timestamp, endTime.Value));

        var filter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;
        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);

        return await _mongoContext.AuditLogs
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(Math.Min(take, 1000)) // Cap at 1000 for performance
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, long>> GetStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default)
    {
        try 
        {
            var matchFilter = new List<BsonDocument>();
    
            if (startTime.HasValue || endTime.HasValue)
            {
                var timestampMatch = new BsonDocument();
                if (startTime.HasValue) timestampMatch["$gte"] = startTime.Value;
                if (endTime.HasValue) timestampMatch["$lte"] = endTime.Value;
                matchFilter.Add(new BsonDocument("timestamp", timestampMatch));
            }
    
            var aggregationStages = new List<BsonDocument>();
    
            if (matchFilter.Any())
            {
                var matchDoc = new BsonDocument("$match", new BsonDocument("$and", new BsonArray(matchFilter)));
                aggregationStages.Add(matchDoc);
            }
    
            var groupStage = new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$serviceName" },
                { "count", new BsonDocument("$sum", 1) },
                { "actions", new BsonDocument("$addToSet", "$action") },
                { "entityTypes", new BsonDocument("$addToSet", "$entityType") }
            });
            
            aggregationStages.Add(groupStage);
    
            var pipeline = aggregationStages;
    
            // Defensive approach for tests
            var cursor = _mongoContext.AuditLogs.Aggregate<BsonDocument>(pipeline);
            
            if (cursor == null)
            {
                _logger.LogWarning("MongoDB aggregate cursor is null for statistics query");
                return new Dictionary<string, long>();
            }
            
            var result = await cursor.ToListAsync(cancellationToken);
            
            var statistics = new Dictionary<string, long>();
            foreach (var item in result)
            {
                var serviceName = item["_id"].AsString;
                var count = item["count"].AsInt64;
                statistics[serviceName] = count;
            }
    
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching statistics from MongoDB");
            return new Dictionary<string, long>();
        }
    }

    public async Task<Dictionary<string, long>> GetActionStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var matchStage = new List<BsonDocument>();
    
            if (startTime.HasValue || endTime.HasValue)
            {
                var timestampMatch = new BsonDocument();
                if (startTime.HasValue) timestampMatch["$gte"] = startTime.Value;
                if (endTime.HasValue) timestampMatch["$lte"] = endTime.Value;
                matchStage.Add(new BsonDocument("timestamp", timestampMatch));
            }
    
            var aggregationStages = new List<BsonDocument>();
    
            if (matchStage.Any())
            {
                var matchDoc = new BsonDocument("$match", new BsonDocument("$and", new BsonArray(matchStage)));
                aggregationStages.Add(matchDoc);
            }
    
            var groupStage = new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$action" },
                { "count", new BsonDocument("$sum", 1) }
            });
            
            aggregationStages.Add(groupStage);
            
            var sortStage = new BsonDocument("$sort", new BsonDocument("count", -1));
            aggregationStages.Add(sortStage);
    
            var pipeline = aggregationStages;
    
            // Defensive approach for tests
            var cursor = _mongoContext.AuditLogs.Aggregate<BsonDocument>(pipeline);
            
            if (cursor == null)
            {
                _logger.LogWarning("MongoDB aggregate cursor is null for action statistics query");
                return new Dictionary<string, long>();
            }
            
            var result = await cursor.ToListAsync(cancellationToken);
            
            var statistics = new Dictionary<string, long>();
            foreach (var item in result)
            {
                var action = item["_id"].AsString;
                var count = item["count"].AsInt64;
                statistics[action] = count;
            }
    
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching action statistics from MongoDB");
            return new Dictionary<string, long>();
        }
    }

    public async Task<Dictionary<string, long>> GetServiceStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetStatisticsAsync(startTime, endTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching service statistics from MongoDB");
            return new Dictionary<string, long>();
        }
    }

    public async Task<long> DeleteOldLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.Lt(x => x.Timestamp, beforeDate);
        
        _logger.LogWarning("Deleting audit logs older than {BeforeDate}", beforeDate);
        
        var result = await _mongoContext.AuditLogs.DeleteManyAsync(filter, cancellationToken);
        
        _logger.LogInformation("Deleted {DeletedCount} audit logs older than {BeforeDate}", 
            result.DeletedCount, beforeDate);
        
        return result.DeletedCount;
    }

    public async Task<long> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _mongoContext.AuditLogs.CountDocumentsAsync(FilterDefinition<AuditLog>.Empty, cancellationToken: cancellationToken);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _mongoContext.AuditLogs.CountDocumentsAsync(
                Builders<AuditLog>.Filter.Empty,
                new CountOptions { Limit = 1 },
                cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log repository health check failed");
            return false;
        }
    }
}