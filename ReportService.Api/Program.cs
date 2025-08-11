using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReportService.Container;
using Serilog;
using Shared.CrossCutting.Extensions;

// Initialize Serilog with basic configuration first
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ReportService application...");

    var builder = WebApplication.CreateBuilder(args);

    // Bind explicitly to 8085 to avoid default 8080
    builder.WebHost.UseUrls("http://+:8085");

    builder.Services.AddControllers();


    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithCorrelationId()
            .Enrich.WithProperty("ServiceName", "ContactService")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/ReportService-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {ServiceName} {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}",
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 7);
    });

    // Configure Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddOpenApi();
    
    // Add HttpClient for health checks or external calls used in health checks
    builder.Services.AddHttpClient();

    // Add application dependencies with async processing (includes MongoDB, MediatR, Kafka, and other dependencies)
    builder.Services.AddReportConsumerDependencies(builder.Configuration);

    // Add correlation ID services
    builder.Services.AddCorrelationId();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

// Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // HTTPS redirection is disabled in container for simplicity/health probes
    // app.UseHttpsRedirection();
    app.MapControllers();

    app.MapGet("/", () => Results.Ok(new { service = "ReportService", status = "ok" }));

    // Health check endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("db") || check.Tags.Contains("messaging"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("app"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    // Health Check UI
    app.MapHealthChecksUI(setup =>
    {
        setup.UIPath = "/health-ui";
        setup.ApiPath = "/health-ui-api";
    });

    Log.Information("ReportService application started successfully");
    Log.Information("ReportService expected port: {Port}", 8085);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}