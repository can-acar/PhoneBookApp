using ContactService.Api.Middleware;
using ContactService.Container;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Serilog.Context;
using System.Diagnostics;
using Shared.CrossCutting.Extensions;
using Shared.CrossCutting.Middleware;

// Initialize Serilog with basic configuration first
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("ContactService uygulaması başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
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
                path: "logs/ContactService-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {ServiceName} {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}",
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 7);
    });

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    // Configure Swagger/OpenAPI
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Contact Service API",
            Version = "v1.0.0",
            Description = "Microservice for managing contacts and contact information in PhoneBookApp",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "PhoneBookApp Team",
                Email = "support@phonebookapp.com"
            }
        });
        
        // Include XML comments for better documentation
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
        
        // Add correlation ID parameter to all operations
        c.OperationFilter<Shared.CrossCutting.Swagger.CorrelationIdOperationFilter>();
        
        // Add response examples
        c.EnableAnnotations();
    });
    
    builder.Services.AddHttpContextAccessor();

    // Add exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();


    // Add application dependencies (includes EntityFramework, MediatR, and other dependencies)
    builder.Services.AddContactServiceDependencies(builder.Configuration);

    // Add correlation ID services
    builder.Services.AddCorrelationId();

    var app = builder.Build();

    // Initialize MongoDB indexes on startup
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var mongoContext = scope.ServiceProvider.GetRequiredService<ContactService.Infrastructure.Data.MongoDbContext>();
            await mongoContext.InitializeIndexesAsync();
            Log.Information("MongoDB indexes initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to initialize MongoDB indexes - service will continue but performance may be impacted");
        }
    }

    // Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Contact Service API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1);
    });

    // Add Serilog request logging
    app.UseSerilogRequestLogging(configure =>
    {
        configure.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        configure.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error 
            : httpContext.Response.StatusCode > 499 
                ? LogEventLevel.Error 
                : LogEventLevel.Information;
        
        configure.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            
            var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
            if (!string.IsNullOrEmpty(userAgent))
            {
                diagnosticContext.Set("UserAgent", userAgent);
            }
            
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(clientIP))
            {
                diagnosticContext.Set("ClientIP", clientIP);
            }
            
            // Add correlation ID to diagnostic context
            if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
            {
                var correlationIdValue = correlationId.FirstOrDefault();
                if (!string.IsNullOrEmpty(correlationIdValue))
                {
                    diagnosticContext.Set("CorrelationId", correlationIdValue);
                }
            }
        };
    });

    // Add exception handling middleware
    app.UseExceptionHandler();

    // Add correlation ID middleware (should be early in pipeline)
    app.UseMiddleware<Shared.CrossCutting.Middleware.CorrelationIdMiddleware>();

    // Add audit logging middleware (after correlation ID)
    app.UseMiddleware<AuditLoggingMiddleware>();

    app.UseHttpsRedirection();
    app.MapControllers();


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
        Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("db"),
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

    Log.Information("ContactService başarıyla başlatıldı");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ContactService başlatılamadı");
}
finally
{
    Log.CloseAndFlush();
}