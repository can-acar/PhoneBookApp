using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ContactService.Infrastructure.Services;

/// <summary>
/// HTTP context-based correlation ID management for ContactService
/// </summary>
public class CorrelationContext : ICorrelationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CorrelationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Try to get from headers first
            if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
            {
                return correlationId.FirstOrDefault();
            }

            // Try to get from response headers if not in request
            if (httpContext.Response.Headers.TryGetValue(CorrelationIdHeaderName, out var responseCorrelationId))
            {
                return responseCorrelationId.FirstOrDefault();
            }

            return null;
        }
    }

    public void SetCorrelationId(string correlationId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        }
    }
}