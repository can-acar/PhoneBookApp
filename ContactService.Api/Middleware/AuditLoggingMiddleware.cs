using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.CrossCutting.CorrelationId;
using System.Text;
using System.Text.Json;

namespace ContactService.Api.Middleware;

/// <summary>
/// Middleware for automatic audit logging of HTTP requests and responses
/// Captures request/response details and logs them to MongoDB for audit trail
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    // Endpoints to exclude from audit logging to reduce noise
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health-ui",
        "/health/ready",
        "/health/live",
        "/metrics",
        "/swagger",
        "/favicon.ico"
    };

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Resolve scoped dependencies per-request via method injection
    public async Task InvokeAsync(
        HttpContext context,
        IAuditLogService auditLogService,
        ICorrelationIdProvider correlationIdProvider)
    {
        // Skip audit logging for excluded paths
        if (ShouldSkipAuditLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var correlationId = correlationIdProvider.CorrelationId;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Capture request details
        var requestBody = await CaptureRequestBodyAsync(context.Request);
        var originalResponseBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Process the request
            await _next(context);

            stopwatch.Stop();

            // Capture response details
            var responseBodyContent = await CaptureResponseBodyAsync(responseBody);
            
            // Copy response back to original stream
            await responseBody.CopyToAsync(originalResponseBodyStream);

            // Log the HTTP request/response audit trail
            await LogHttpRequestAsync(
                correlationId,
                context,
                requestBody,
                responseBodyContent,
                stopwatch.ElapsedMilliseconds,
                auditLogService);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log the failed request
            await LogHttpRequestAsync(
                correlationId,
                context,
                requestBody,
                ex.Message,
                stopwatch.ElapsedMilliseconds,
                auditLogService,
                ex);

            // Restore original response stream
            context.Response.Body = originalResponseBodyStream;
            throw;
        }
    }

    private static bool ShouldSkipAuditLogging(PathString path)
    {
        return ExcludedPaths.Any(excludedPath => 
            path.Value?.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static async Task<string> CaptureRequestBodyAsync(HttpRequest request)
    {
        try
        {
            if (!request.ContentLength.HasValue || request.ContentLength == 0)
                return string.Empty;

            // Only capture text-based content types
            var contentType = request.ContentType?.ToLowerInvariant();
            if (contentType == null || 
                (!contentType.Contains("json") && 
                 !contentType.Contains("xml") && 
                 !contentType.Contains("text")))
            {
                return $"[Binary Content: {contentType}]";
            }

            request.EnableBuffering();
            var body = request.Body;
            body.Position = 0;

            using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            
            body.Position = 0;
            
            // Limit body size for logging
            return requestBody.Length > 4000 ? 
                requestBody.Substring(0, 4000) + "... [truncated]" : 
                requestBody;
        }
        catch (Exception)
        {
            return "[Error reading request body]";
        }
    }

    private static async Task<string> CaptureResponseBodyAsync(MemoryStream responseBody)
    {
        try
        {
            responseBody.Position = 0;
            using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
            var responseContent = await reader.ReadToEndAsync();
            responseBody.Position = 0;

            // Limit response size for logging
            return responseContent.Length > 4000 ? 
                responseContent.Substring(0, 4000) + "... [truncated]" : 
                responseContent;
        }
        catch (Exception)
        {
            return "[Error reading response body]";
        }
    }

    private async Task LogHttpRequestAsync(
        string correlationId,
        HttpContext context,
        string requestBody,
        string responseBody,
        long durationMs,
        IAuditLogService auditLogService,
        Exception? exception = null)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            var metadata = new Dictionary<string, object>
            {
                ["httpMethod"] = request.Method,
                ["requestPath"] = request.Path.Value ?? "",
                ["queryString"] = request.QueryString.Value ?? "",
                ["statusCode"] = response.StatusCode,
                ["durationMs"] = durationMs,
                ["contentLength"] = response.ContentLength ?? 0,
                ["userAgent"] = request.Headers.UserAgent.ToString(),
                ["referer"] = request.Headers.Referer.ToString(),
                ["protocol"] = request.Protocol
            };

            if (exception != null)
            {
                metadata["hasException"] = true;
                metadata["exceptionType"] = exception.GetType().Name;
            }

            var auditData = new
            {
                request = new
                {
                    method = request.Method,
                    path = request.Path.Value,
                    query = request.QueryString.Value,
                    headers = GetSafeHeaders(request.Headers),
                    body = requestBody,
                    contentType = request.ContentType,
                    contentLength = request.ContentLength
                },
                response = new
                {
                    statusCode = response.StatusCode,
                    body = responseBody,
                    contentType = response.ContentType,
                    contentLength = response.ContentLength
                },
                performance = new
                {
                    durationMs = durationMs,
                    timestamp = DateTime.UtcNow
                }
            };

            await auditLogService.LogActionAsync(
                correlationId: correlationId,
                action: "HTTP_REQUEST",
                entityType: "HttpRequest",
                entityId: $"{request.Method}:{request.Path}",
                data: auditData,
                userId: GetUserIdFromContext(context),
                ipAddress: GetClientIpAddress(context),
                userAgent: request.Headers.UserAgent.ToString(),
                metadata: metadata);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Failed to log HTTP request audit trail for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
    }

    private static Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        // Filter out sensitive headers
        var sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Cookie", "X-API-Key", "X-Auth-Token"
        };

        var safeHeaders = new Dictionary<string, string>();
        foreach (var header in headers)
        {
            if (!sensitiveHeaders.Contains(header.Key))
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
            else
            {
                safeHeaders[header.Key] = "[REDACTED]";
            }
        }

        return safeHeaders;
    }

    private static string? GetUserIdFromContext(HttpContext context)
    {
        // Try to extract user ID from various sources
        var userId = context.User?.Identity?.Name;
        
        if (string.IsNullOrEmpty(userId))
        {
            userId = context.User?.FindFirst("sub")?.Value ??
                     context.User?.FindFirst("userId")?.Value ??
                     context.User?.FindFirst("id")?.Value;
        }

        return userId;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}