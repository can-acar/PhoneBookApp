using ReportService.Domain.Entities;
using ReportService.Domain.Enums;

namespace ReportService.Domain.Interfaces;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Report>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Report>> GetByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default);
    Task<Report> CreateAsync(Report report, CancellationToken cancellationToken = default);
    Task<Report> UpdateAsync(Report report, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}