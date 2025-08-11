using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using NotificationService.ApplicationService.Worker;
using NotificationService.ApplicationService.Handlers.Commands;
using NotificationService.ApplicationService.Handlers.Queries;
using NotificationService.ApplicationService.Models;
using NotificationService.ApplicationService.Services;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Repositories;
using System.Reflection;
using NotificationService.Infrastructure.Providers;
using NotificationService.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NotificationService.ApplicationService.Communicator;
using Shared.CrossCutting.CorrelationId;
using Shared.CrossCutting.Extensions;

namespace NotificationService.Container
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure MongoDB
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));
            services.AddSingleton<MongoDbContext>();

            // Configure Kafka
            services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));

            // Configure Email Provider
            services.Configure<EmailProviderSettings>(configuration.GetSection("EmailProvider"));

            // Configure SMS Provider
            services.Configure<SmsProviderSettings>(configuration.GetSection("SmsProvider"));

            // Register repositories
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();

            // Register services
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<INotificationService, ApplicationService.Services.NotificationService>();

            // Add correlation ID support
            services.AddCorrelationId();

            // Contact Service GraphQL Client
            var contactServiceUrl = configuration.GetValue<string>("ContactService:Url") ?? "https://localhost:5000";
            services.AddScoped<NotificationService.Domain.Interfaces.IContactServiceClient>(provider =>
            {
                var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NotificationContactServiceClient>>();
                var correlationIdProvider = provider.GetRequiredService<ICorrelationIdProvider>();
                return new NotificationContactServiceClient(
                    contactServiceUrl, logger, correlationIdProvider);
            });

            // Register notification providers
            services.AddScoped<INotificationProvider, EmailProvider>();
            services.AddScoped<INotificationProvider, SmsProvider>();
            services.AddScoped<INotificationProviderManager, NotificationProviderManager>();

            // Register HttpClient for SMS provider
            services.AddHttpClient<SmsProvider>();

            // Register background services
            services.AddHostedService<ReportCompletedWorker>();

            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SendNotificationCommand).Assembly));

            // Add Health Checks
            AddHealthChecks(services, configuration);

            return services;
        }

        /// <summary>
        /// Configure health checks for NotificationService
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
                .AddCheck<NotificationProvidersHealthCheck>(
                    name: "notification-providers",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "providers", "notifications" })
                // MongoDB health check
                .AddMongoDb(
                    _ => new MongoDB.Driver.MongoClient(configuration.GetSection("MongoDb:ConnectionString").Value ?? "mongodb://localhost:27017"),
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
                setup.AddHealthCheckEndpoint("Notification Service", "/health");
                
                // If other services are available, add them here
                // setup.AddHealthCheckEndpoint("Contact Service", "http://contact-service/health");
                // setup.AddHealthCheckEndpoint("Report Service", "http://report-service/health");
            })
            .AddInMemoryStorage();
        }
    }
}
