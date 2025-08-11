namespace ContactService.Domain.Interfaces;

/// <summary>
/// Provides access to the current correlation ID for request tracking
/// </summary>
public interface ICorrelationContext
{
    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string? CorrelationId { get; }
    
    /// <summary>
    /// Sets the correlation ID
    /// </summary>
    /// <param name="correlationId">The correlation ID to set</param>
    void SetCorrelationId(string correlationId);
}