
using ContactService.ApplicationService.Behaviors;
using ContactService.ApplicationService.Services;
using ContactService.ApplicationService.Worker;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Configuration;
using ContactService.Infrastructure.Data;
using ContactService.Infrastructure.HealthChecks;
using ContactService.Infrastructure.Repositories;
using ContactService.Infrastructure.Services;
using StackExchange.Redis;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ContactService.Container;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContactServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Kafka settings
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
        
        // Add Entity Framework
        services.AddDbContext<ContactDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        // Register repositories
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IContactHistoryRepository, ContactHistoryRepository>();
        
        // Configure Redis
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        
        // Add Redis distributed cache
        var redisSettings = new RedisSettings();
        configuration.GetSection("Redis").Bind(redisSettings);
        
        // Register base contact service concrete
    services.AddScoped<ContactService.ApplicationService.Services.ContactService>();
    // Expose the base service via IContactServiceCore to target undecorated implementation
    services.AddScoped<IContactServiceCore>(sp => sp.GetRequiredService<ContactService.ApplicationService.Services.ContactService>());

        if (redisSettings.Enabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                // Allow startup even if Redis is not yet available
                options.Configuration = string.IsNullOrWhiteSpace(redisSettings.ConnectionString)
                    ? redisSettings.ConnectionString
                    : (redisSettings.ConnectionString.Contains("abortConnect=false", StringComparison.OrdinalIgnoreCase)
                        ? redisSettings.ConnectionString
                        : $"{redisSettings.ConnectionString},abortConnect=false");
                options.InstanceName = redisSettings.InstanceName;
            });
            
            // Add Redis connection multiplexer
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var conn = string.IsNullOrWhiteSpace(redisSettings.ConnectionString)
                    ? redisSettings.ConnectionString
                    : (redisSettings.ConnectionString.Contains("abortConnect=false", StringComparison.OrdinalIgnoreCase)
                        ? redisSettings.ConnectionString
                        : $"{redisSettings.ConnectionString},abortConnect=false");
                return ConnectionMultiplexer.Connect(conn);
            });
            
            // Add cache service
            services.AddScoped<ICacheService, RedisCacheService>();

            // Decorate IContactService with CachedContactService while exposing ICachedContactService too
            services.AddScoped<ICachedContactService, CachedContactService>(sp =>
            {
                var baseSvc = sp.GetRequiredService<IContactServiceCore>();
                var cache = sp.GetRequiredService<ICacheService>();
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisSettings>>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedContactService>>();
                return new CachedContactService(baseSvc, cache, opts, logger);
            });

            services.AddScoped<IContactService>(sp =>
            {
                // Resolve the cached service as the implementation for IContactService
                return (IContactService)sp.GetRequiredService<ICachedContactService>();
            });
        }
        else
        {
            // When caching is disabled, expose IContactService via passthrough adapter over the core service
            services.AddScoped<IContactService, PassthroughContactService>();
        }
        
        // Register application services
        services.AddScoped<IContactHistoryService, ContactHistoryService>();
        services.AddScoped<IOutboxService, OutboxService>();
        
        // Register infrastructure services
        services.AddScoped<IKafkaProducer, KafkaProducer>();
        services.AddScoped<CommunicationInfoService>();
        
        // Add MongoDB context and audit logging
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IAuditLogRepository, MongoAuditLogRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        
        // Register background services
        services.AddHostedService<OutboxProcessorWorker>();
        
        // Validate Kafka configuration on startup
        services.AddOptions<KafkaSettings>()
            .BindConfiguration(KafkaSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // Register HTTP context accessor for correlation ID
        services.AddHttpContextAccessor();
        
        // Register correlation context (extends shared correlation ID provider)
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        
        // Add FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApiContract.AssemblyReference).Assembly);
        
        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationService.AssemblyReference).Assembly));

        // Add MediatR behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CorrelationBehavior<,>));

        // Add health checks
        services.AddHealthChecks()
            // Database health checks
            .AddDbContextCheck<ContactDbContext>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "postgresql" })
            
            // Custom health checks
            .AddCheck<ApplicationHealthCheck>(
                name: "application", 
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "app", "self" })
            
            .AddCheck<KafkaHealthCheck>(
                name: "kafka",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "kafka", "messaging" })
            
            .AddCheck<OutboxHealthCheck>(
                name: "outbox",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "outbox", "messaging" })
            
            .AddCheck<RedisHealthCheck>(
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "cache", "redis" })
            
            // MongoDB health check (for communication info and audit logs)
            .AddMongoDb(
                _ => new MongoDB.Driver.MongoClient(configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017"),
                name: "mongodb",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "db", "mongodb" })
            
            // Audit logging health check
            .AddCheck<AuditLogHealthCheck>(
                name: "audit-logging",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "audit", "logging", "mongodb" });

        // Health Check UI
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(30);
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.SetMinimumSecondsBetweenFailureNotifications(60);
            
            // Configure health check endpoints for UI
            // Use relative URL within same service
            setup.AddHealthCheckEndpoint("Contact Service", "/health");
        })
        .AddInMemoryStorage();

        return services;
    }
}