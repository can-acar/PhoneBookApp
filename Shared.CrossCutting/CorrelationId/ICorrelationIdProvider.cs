using Microsoft.Extensions.Primitives;

namespace Shared.CrossCutting.CorrelationId;

/// <summary>
/// Provides correlation ID functionality for request tracing across services
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets the correlation ID for the current context
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new correlation ID if one doesn't exist
    /// </summary>
    void EnsureCorrelationId();

    /// <summary>
    /// Extracts correlation ID from headers (simplified version)
    /// </summary>
    void ExtractFromHeaders(IDictionary<string, StringValues> headers);

    /// <summary>
    /// Adds correlation ID to headers (simplified version)
    /// </summary>
    void AddToHeaders(IDictionary<string, StringValues> headers);

    /// <summary>
    /// Gets the current correlation ID as string
    /// </summary>
    string Get();
}
