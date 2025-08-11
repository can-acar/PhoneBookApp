using Serilog.Context;

namespace ContactService.Api.Middleware;

/// <summary>
/// ContactService Correlation ID middleware for request tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Set correlation ID in response header
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        
        // Add correlation ID to Serilog context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation("İstek başlatıldı: {Method} {Path} - CorrelationId: {CorrelationId}", 
                context.Request.Method, 
                context.Request.Path, 
                correlationId);
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstek sırasında hata oluştu - CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
            finally
            {
                _logger.LogInformation("İstek tamamlandı: {Method} {Path} - StatusCode: {StatusCode} - CorrelationId: {CorrelationId}", 
                    context.Request.Method, 
                    context.Request.Path, 
                    context.Response.StatusCode,
                    correlationId);
            }
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID if none exists
        return Guid.NewGuid().ToString();
    }
}