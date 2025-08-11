namespace ReportService.Domain.Interfaces;

/// <summary>
/// Report generation service interface for clean architecture
/// Domain layer: Defines contract for report generation business logic
/// </summary>
public interface IReportGenerationService
{
    /// <summary>
    /// Generate report with location and user context
    /// </summary>
    /// <param name="reportId">Unique report identifier</param>
    /// <param name="location">Location for filtering contacts</param>
    /// <param name="userId">User requesting the report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task GenerateReportAsync(Guid reportId, string location, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate report with minimal parameters (legacy support)
    /// </summary>
    /// <param name="reportId">Unique report identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task GenerateReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if report exists
    /// </summary>
    /// <param name="reportId">Report identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> ReportExistsAsync(Guid reportId, CancellationToken cancellationToken = default);
}
