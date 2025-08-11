using ReportService.Domain.Entities;

namespace ReportService.Domain.Interfaces;

public interface IReportGenerator
{
    Task<ReportGenerationResult> GenerateLocationReportAsync(string location, CancellationToken cancellationToken = default);
    Task GenerateReportAsync(Guid reportId, CancellationToken cancellationToken = default);
}