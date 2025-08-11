using ContactService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ContactService.Infrastructure.Data;

/// <summary>
/// MongoDB context for audit logging and document storage
/// Handles connection to MongoDB instance for audit trail and logging data
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbContext> _logger;

    public MongoDbContext(IConfiguration configuration, ILogger<MongoDbContext> logger)
    {
        _logger = logger;

        // Try to get connection string using both methods for compatibility
        string? connectionString = configuration.GetConnectionString("MongoDB");
        if (connectionString == null)
        {
            // Fallback to direct section access
            connectionString = configuration.GetSection("ConnectionStrings:MongoDB").Value;
        }

        if (connectionString == null)
        {
            throw new InvalidOperationException("MongoDB connection string is required");
        }

        var mongoUrl = new MongoUrl(connectionString);
        var client = new MongoClient(mongoUrl);
        
        _database = client.GetDatabase(mongoUrl.DatabaseName ?? "ContactServiceDb");

        _logger.LogInformation("Connected to MongoDB database: {DatabaseName}", _database.DatabaseNamespace.DatabaseName);
    }

    /// <summary>
    /// Audit logs collection
    /// </summary>
    public virtual IMongoCollection<AuditLog> AuditLogs => _database.GetCollection<AuditLog>("AuditLogs");

    /// <summary>
    /// Communication information collection (if needed for cross-references)
    /// </summary>
    public virtual IMongoCollection<CommunicationInfo> CommunicationInfos => _database.GetCollection<CommunicationInfo>("CommunicationInformation");

    /// <summary>
    /// System logs collection for structured logging
    /// </summary>
    public virtual IMongoCollection<SystemLog> SystemLogs => _database.GetCollection<SystemLog>("SystemLogs");

    /// <summary>
    /// Performance metrics collection
    /// </summary>
    public virtual IMongoCollection<PerformanceMetric> PerformanceMetrics => _database.GetCollection<PerformanceMetric>("PerformanceMetrics");

    /// <summary>
    /// Get MongoDB database instance for direct operations
    /// </summary>
    public IMongoDatabase Database => _database;

    /// <summary>
    /// Initialize database indexes for optimal query performance
    /// </summary>
    public async Task InitializeIndexesAsync()
    {
        _logger.LogInformation("Creating MongoDB indexes for optimal query performance...");

        try
        {
            // AuditLogs indexes
            var auditLogIndexes = new[]
            {
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.CorrelationId),
                    new CreateIndexOptions { Name = "idx_correlationId", Background = true }),

                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.EntityType).Ascending(x => x.EntityId),
                    new CreateIndexOptions { Name = "idx_entity", Background = true }),

                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.UserId),
                    new CreateIndexOptions { Name = "idx_userId", Background = true, Sparse = true }),

                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.ServiceName),
                    new CreateIndexOptions { Name = "idx_serviceName", Background = true }),

                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.Action),
                    new CreateIndexOptions { Name = "idx_action", Background = true }),

                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Descending(x => x.Timestamp),
                    new CreateIndexOptions { Name = "idx_timestamp", Background = true }),

                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.Timestamp).Ascending(x => x.ServiceName),
                    new CreateIndexOptions { Name = "idx_timestamp_service", Background = true }),

                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.IpAddress),
                    new CreateIndexOptions { Name = "idx_ipAddress", Background = true, Sparse = true })
            };

            await AuditLogs.Indexes.CreateManyAsync(auditLogIndexes);

            // SystemLogs indexes
            var systemLogIndexes = new[]
            {
                new CreateIndexModel<SystemLog>(
                    Builders<SystemLog>.IndexKeys.Descending(x => x.Timestamp),
                    new CreateIndexOptions { Name = "idx_systemlog_timestamp", Background = true }),

                new CreateIndexModel<SystemLog>(
                    Builders<SystemLog>.IndexKeys.Ascending(x => x.Level),
                    new CreateIndexOptions { Name = "idx_systemlog_level", Background = true }),

                new CreateIndexModel<SystemLog>(
                    Builders<SystemLog>.IndexKeys.Ascending(x => x.CorrelationId),
                    new CreateIndexOptions { Name = "idx_systemlog_correlationId", Background = true, Sparse = true })
            };

            await SystemLogs.Indexes.CreateManyAsync(systemLogIndexes);

            // PerformanceMetrics indexes
            var performanceIndexes = new[]
            {
                new CreateIndexModel<PerformanceMetric>(
                    Builders<PerformanceMetric>.IndexKeys.Descending(x => x.Timestamp),
                    new CreateIndexOptions { Name = "idx_performance_timestamp", Background = true }),

                new CreateIndexModel<PerformanceMetric>(
                    Builders<PerformanceMetric>.IndexKeys.Ascending(x => x.MetricType),
                    new CreateIndexOptions { Name = "idx_performance_type", Background = true })
            };

            await PerformanceMetrics.Indexes.CreateManyAsync(performanceIndexes);

            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes");
            throw;
        }
    }

    /// <summary>
    /// Test MongoDB connection health
    /// </summary>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB health check failed");
            return false;
        }
    }
}

/// <summary>
/// Communication information document model for MongoDB
/// </summary>
public class CommunicationInfo
{
    public string? Id { get; set; }
    public string ContactId { get; set; } = string.Empty;
    public string CommunicationType { get; set; } = string.Empty;
    public string CommunicationContent { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// System log document model for structured logging in MongoDB
/// </summary>
public class SystemLog
{
    public string? Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string ServiceName { get; set; } = "ContactService";
    public string? SourceContext { get; set; }
    public Exception? Exception { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Performance metric document model for monitoring
/// </summary>
public class PerformanceMetric
{
    public string? Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MetricType { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Unit { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, object> Tags { get; set; } = new();
}