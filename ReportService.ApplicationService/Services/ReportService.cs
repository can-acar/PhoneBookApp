using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Interfaces;

namespace ReportService.ApplicationService.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _reportRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _reportRepository.GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default)
    {
        return await _reportRepository.GetByStatusAsync(status, cancellationToken);
    }

    public async Task<Report> CreateAsync(Report report, CancellationToken cancellationToken = default)
    {
        return await _reportRepository.CreateAsync(report, cancellationToken);
    }

    public async Task<Report> UpdateAsync(Report report, CancellationToken cancellationToken = default)
    {
        return await _reportRepository.UpdateAsync(report, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _reportRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _reportRepository.ExistsAsync(id, cancellationToken);
    }
}
