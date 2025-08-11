using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.CrossCutting.CorrelationId;
using System.Linq;
using Shared.CrossCutting.Constants;

namespace Shared.CrossCutting.Middleware;

/// <summary>
/// Middleware to handle correlation ID propagation across HTTP requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdProvider correlationIdProvider)
    {
        var correlationId = ExtractOrGenerateCorrelationId(context, correlationIdProvider);
        
        // Set correlation ID in provider
        correlationIdProvider.SetCorrelationId(correlationId);
        
        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdConstants.CORRELATION_ID_HEADER))
            {
                context.Response.Headers.Add(CorrelationIdConstants.CORRELATION_ID_HEADER, correlationId);
            }
            return Task.CompletedTask;
        });

        // Add to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdConstants.CORRELATION_ID_LOG_KEY] = correlationId,
            ["RequestPath"] = context.Request.Path.Value ?? "",
            ["HttpMethod"] = context.Request.Method,
            ["ServiceName"] = GetServiceName()
        }))
        {
            _logger.LogDebug("Processing request with correlation ID: {CorrelationId}", correlationId);
            
            await _next(context);
            
            _logger.LogDebug("Completed request with correlation ID: {CorrelationId}", correlationId);
        }
    }

    private string ExtractOrGenerateCorrelationId(HttpContext context, ICorrelationIdProvider correlationIdProvider)
    {
        // Try to extract from headers using the provider's method
        correlationIdProvider.ExtractFromHeaders(context.Request.Headers.ToDictionary(h => h.Key, h => h.Value));
        
        // Return the correlation ID (it will be generated if not found in headers)
        var correlationId = correlationIdProvider.CorrelationId;
        
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            _logger.LogDebug("Using correlation ID: {CorrelationId}", correlationId);
            return correlationId;
        }

        // Fallback - generate new correlation ID if somehow still empty
        var newCorrelationId = Guid.NewGuid().ToString("N")[..16];
        correlationIdProvider.SetCorrelationId(newCorrelationId);
        _logger.LogDebug("Generated new correlation ID: {CorrelationId}", newCorrelationId);
        
        return newCorrelationId;
    }

    private string GetServiceName()
    {
        // Try to get service name from environment variables or configuration
        return Environment.GetEnvironmentVariable("SERVICE_NAME") ?? 
               Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME") ?? 
               "UnknownService";
    }
}
