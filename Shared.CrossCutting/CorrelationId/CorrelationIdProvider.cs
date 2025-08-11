using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Shared.CrossCutting.Constants;

namespace Shared.CrossCutting.CorrelationId;



/// <summary>
/// HTTP context-based correlation ID provider
/// </summary>
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _correlationId;

    public CorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId
    {
        get
        {
            if (!string.IsNullOrEmpty(_correlationId))
                return _correlationId;

            // Try to get from HTTP context first
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Check if it's already in context items
                if (httpContext.Items.TryGetValue(CorrelationIdConstants.CORRELATION_ID_CONTEXT_KEY, out var contextValue) &&
                    contextValue is string contextCorrelationId &&
                    !string.IsNullOrEmpty(contextCorrelationId))
                {
                    _correlationId = contextCorrelationId;
                    return _correlationId;
                }

                // Extract from headers
                var headerCorrelationId = ExtractFromHttpHeaders(httpContext.Request.Headers);
                if (!string.IsNullOrEmpty(headerCorrelationId))
                {
                    SetCorrelationId(headerCorrelationId);
                    return _correlationId!;
                }
            }

            // Generate new correlation ID if none found
            EnsureCorrelationId();
            return _correlationId!;
        }
    }

    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

        _correlationId = correlationId;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items[CorrelationIdConstants.CORRELATION_ID_CONTEXT_KEY] = correlationId;
            
            // Add to response headers for client visibility
            if (!httpContext.Response.Headers.ContainsKey(CorrelationIdConstants.CORRELATION_ID_HEADER))
            {
                httpContext.Response.Headers.Add(CorrelationIdConstants.CORRELATION_ID_HEADER, correlationId);
            }
        }
    }

    public void EnsureCorrelationId()
    {
        if (string.IsNullOrEmpty(_correlationId))
        {
            var newCorrelationId = Guid.NewGuid().ToString("N")[..16]; // Short 16-character ID
            SetCorrelationId(newCorrelationId);
        }
    }

    public void ExtractFromHeaders(IDictionary<string, StringValues> headers)
    {
        if (headers.TryGetValue(CorrelationIdConstants.CORRELATION_ID_HEADER, out var correlationIdValue))
        {
            var correlationId = correlationIdValue.ToString();
            if (!string.IsNullOrEmpty(correlationId))
            {
                SetCorrelationId(correlationId);
                return;
            }
        }
        
        // Generate new correlation ID if not found in headers
        EnsureCorrelationId();
    }

    public void AddToHeaders(IDictionary<string, StringValues> headers)
    {
        var correlationId = CorrelationId;
        if (!string.IsNullOrEmpty(correlationId))
        {
            headers[CorrelationIdConstants.CORRELATION_ID_HEADER] = correlationId;
        }
    }

    // Helper method for HTTP headers
    private string? ExtractFromHttpHeaders(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(CorrelationIdConstants.CORRELATION_ID_HEADER, out var correlationId))
        {
            var id = correlationId.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }
        return null;
    }

    // Helper method for HTTP headers
    public void AddToHttpHeaders(IHeaderDictionary headers)
    {
        var correlationId = CorrelationId;
        if (!headers.ContainsKey(CorrelationIdConstants.CORRELATION_ID_HEADER))
        {
            headers.Add(CorrelationIdConstants.CORRELATION_ID_HEADER, correlationId);
        }
    }

    public string Get()
    {
        return CorrelationId;
    }
}
