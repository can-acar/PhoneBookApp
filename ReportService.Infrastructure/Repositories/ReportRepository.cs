using MongoDB.Driver;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Interfaces;
using ReportService.Infrastructure.Data;

namespace ReportService.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly IMongoCollection<Report> _reports;

    public ReportRepository(IReportMongoContext context)
    {
        _reports = context.Reports;
    }

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _reports
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _reports
            .Find(_ => true)
            .SortByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default)
    {
        return await _reports
            .Find(r => r.Status == status)
            .SortByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Report> CreateAsync(Report report, CancellationToken cancellationToken = default)
    {
        // No need to set Id or RequestedAt as they're already set in the constructor
        await _reports.InsertOneAsync(report, cancellationToken: cancellationToken);
        return report;
    }

    public async Task<Report> UpdateAsync(Report report, CancellationToken cancellationToken = default)
    {
        await _reports.ReplaceOneAsync(
            r => r.Id == report.Id, 
            report, 
            cancellationToken: cancellationToken);
        return report;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _reports.DeleteOneAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var count = await _reports.CountDocumentsAsync(r => r.Id == id, cancellationToken: cancellationToken);
        return count > 0;
    }
}