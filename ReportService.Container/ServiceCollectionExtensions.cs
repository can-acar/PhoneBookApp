using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReportService.ApplicationService.Services;
using ReportService.Domain.Interfaces;
using ReportService.Infrastructure.Configuration;
using ReportService.Infrastructure.Data;
using ReportService.Infrastructure.Repositories;
using ReportService.Infrastructure.Services;
using ReportService.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReportService.ApplicationService.Worker;
using ReportService.Infrastructure.Consumers;
using ReportService.Infrastructure.Publisher;
using Shared.CrossCutting.CorrelationId;
using Shared.CrossCutting.Extensions;

namespace ReportService.Container;

/// <summary>
/// Service collection extensions following Clean Architecture
/// Container layer: Responsible for dependency injection configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register dependencies for Report Consumer background service
    /// Clean Architecture: Infrastructure layer registration for async processing
    /// </summary>
    public static IServiceCollection AddReportConsumerDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // First, add the base dependencies
        services.AddReportServiceDependencies(configuration);

        // Add background service for async processing
        //services.AddHostedService<ReportConsumerWorker>();

        services.AddHostedService<ReportProcessingWorker>();
        services.AddHostedService<ReportService.ApplicationService.Worker.ReportConsumerWorker>();


        return services;
    }

    /// <summary>
    /// Register core Report Service dependencies
    /// Clean Architecture: All service registrations with proper lifetimes
    /// </summary>
    public static IServiceCollection AddReportServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDB (match appsettings keys: MongoDbSettings__ConnectionString/DatabaseName)
        var mongoSettings = new MongoDbSettings();
        configuration.GetSection("MongoDbSettings").Bind(mongoSettings);
        services.AddSingleton(mongoSettings);

        // Configure Kafka settings (IOptions pattern)
        services.Configure<ReportService.Infrastructure.Configuration.KafkaSettings>(configuration.GetSection("Kafka"));
        services.PostConfigure<ReportService.Infrastructure.Configuration.KafkaSettings>(opts =>
        {
            // If flat topic keys are present, propagate to nested Topics for backward compatibility
            if (!string.IsNullOrWhiteSpace(opts.ReportRequestsTopic))
            {
                opts.Topics.ReportRequests = opts.ReportRequestsTopic;
            }
            if (!string.IsNullOrWhiteSpace(opts.ReportCompletedTopic))
            {
                opts.Topics.ReportCompleted = opts.ReportCompletedTopic;
            }
        });

        // Data context - Scoped for per-request lifecycle
        services.AddScoped<IReportMongoContext, ReportMongoContext>();

        // Repository registrations - Scoped for proper unit of work
        services.AddScoped<IReportRepository, ReportRepository>();

        // Application Service registrations - Scoped for per-request
        services.AddScoped<IReportService, ReportService.ApplicationService.Services.ReportService>();
        services.AddScoped<IReportGenerationService, ReportGenerationService>();

        // Infrastructure services - Singleton for event publishing
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        

        // Shared Cross-cutting Concerns
        services.AddCorrelationId();

        // Contact Service Client (HTTP client for inter-service communication)
        var contactServiceUrl = configuration.GetValue<string>("ContactService:Url") ?? "https://localhost:5000";

        // Add HttpClient for ContactService
        services.AddHttpClient<IContactServiceClient, ContactServiceClient>(client =>
        {
            client.BaseAddress = new Uri(contactServiceUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "ReportService/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IContactServiceClient, ContactServiceClient>();

        // Add HTTP Context Accessor for correlation ID tracking
        // Note: Will be added in API project, not needed in Consumer
        services.AddHttpContextAccessor();

        // Add MediatR with all behaviors
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationService.AssemblyReference).Assembly));

        // Add Health Checks
        AddHealthChecks(services, configuration);

        return services;
    }

    /// <summary>
    /// Configure health checks for ReportService
    /// </summary>
    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        // Register core health checks
        services.AddHealthChecks()
            // Custom health checks
            .AddCheck<ApplicationHealthCheck>(
                name: "application",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "app", "self" })
            .AddCheck<KafkaHealthCheck>(
                name: "kafka",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "kafka", "messaging" })
            .AddCheck<ContactServiceHealthCheck>(
                name: "contact-service",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "external", "contact-service" })
            // MongoDB health check - align with MongoDbSettings used by the service configuration
            .AddMongoDb(
                _ =>
                {
                    var conn = configuration.GetSection("MongoDbSettings:ConnectionString").Value
                               ?? configuration.GetValue<string>("MongoDbSettings__ConnectionString")
                               ?? "mongodb://localhost:27017";
                    return new MongoDB.Driver.MongoClient(conn);
                },
                name: "mongodb",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "db", "mongodb" });

        // Health Check UI
        services.AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(30);
                setup.MaximumHistoryEntriesPerEndpoint(50);
                setup.SetMinimumSecondsBetweenFailureNotifications(60);

                // Configure health check endpoints for UI
                setup.AddHealthCheckEndpoint("Report Service", "/health");

                // If other services are available, add them here
                // setup.AddHealthCheckEndpoint("Contact Service", "http://contact-service/health");
                // setup.AddHealthCheckEndpoint("Notification Service", "http://notification-service/health");
            })
            .AddInMemoryStorage();
    }
}