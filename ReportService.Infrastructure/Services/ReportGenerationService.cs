using Microsoft.Extensions.Logging;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Events;
using ReportService.Domain.Interfaces;
using System.Text.Json;

namespace ReportService.Infrastructure.Services;

public class ReportGenerationService : IReportGenerationService
{
    private readonly IReportRepository _reportRepository;
    private readonly IContactServiceClient _contactServiceClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ReportGenerationService> _logger;

    public ReportGenerationService(
        IReportRepository reportRepository,
        IContactServiceClient contactServiceClient,
        IEventPublisher eventPublisher,
        ILogger<ReportGenerationService> logger)
    {
        _reportRepository = reportRepository;
        _contactServiceClient = contactServiceClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task GenerateReportAsync(Guid reportId, string location, string userId, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report == null)
        {
            _logger.LogWarning("Report with ID {ReportId} not found", reportId);
            return;
        }

        if (report.Status != ReportStatus.Preparing)
        {
            _logger.LogWarning("Report {ReportId} is not in Preparing status. Current status: {Status}", 
                reportId, report.Status);
            return;
        }

        _logger.LogInformation("Starting report generation for Report ID: {ReportId}, Location: {Location}, User: {UserId}", 
            reportId, location, userId);

        try
        {
            // Update status to InProgress
            report.MarkAsInProgress();
            await _reportRepository.UpdateAsync(report, cancellationToken);

            // Use provided location or fallback to report's location
            var targetLocation = !string.IsNullOrWhiteSpace(location) ? location : report.Location;

            // Generate location statistics
            var locationStatistics = await GenerateLocationStatisticsAsync(targetLocation, cancellationToken);

            // Add location statistics to report
            foreach (var stat in locationStatistics)
            {
                if (!report.HasLocationStatistic(stat.Location))
                {
                    report.AddLocationStatistic(stat.Location, stat.PersonCount, stat.PhoneNumberCount);
                }
            }
            
            // Generate report file (JSON format)
            var reportData = await GenerateReportFileAsync(report, cancellationToken);
            report.UpdateFileInformation(reportData.FilePath, reportData.Format, reportData.SizeBytes);
            
            // Mark report as completed with statistics
            int totalPersonCount = locationStatistics.Sum(ls => ls.PersonCount);
            int totalPhoneNumberCount = locationStatistics.Sum(ls => ls.PhoneNumberCount);
            report.MarkAsCompleted(totalPersonCount, totalPhoneNumberCount, reportData.FilePath, reportData.SizeBytes);

            await _reportRepository.UpdateAsync(report, cancellationToken);

            _logger.LogInformation("Report generation completed for Report ID: {ReportId}", reportId);

            // Publish completion event
            await PublishReportCompletedEventAsync(report, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for Report ID: {ReportId}", reportId);

            // Update report with error status
            report.MarkAsFailed(ex.Message);

            await _reportRepository.UpdateAsync(report, cancellationToken);

            // Publish failure event
            await PublishReportCompletedEventAsync(report, cancellationToken);
        }
    }

    public async Task GenerateReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        // Legacy overload - get report and use its location
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report == null)
        {
            _logger.LogWarning("Report with ID {ReportId} not found", reportId);
            return;
        }

        // Call the full implementation with report's stored data
        await GenerateReportAsync(reportId, report.Location, report.RequestedBy, cancellationToken);
    }

    public async Task<bool> ReportExistsAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        return report != null;
    }

    private async Task<List<LocationStatistic>> GenerateLocationStatisticsAsync(string location, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating location statistics for location: {Location}", location);

            // Call Contact Service via gRPC to get location-based statistics
            var contacts = await _contactServiceClient.GetContactsByLocationAsync(location, cancellationToken);

            var locationStatistics = new List<LocationStatistic>();

            // If specific location is requested, get data for that location
            if (!string.IsNullOrWhiteSpace(location))
            {
                int personCount = contacts.Count();
                int phoneNumberCount = contacts.SelectMany(c => c.ContactInfos?.Where(ci => ci.Type == "PHONE") ?? Array.Empty<ContactInfo>()).Count();
                var locationStat = new LocationStatistic(location, personCount, phoneNumberCount);
                locationStatistics.Add(locationStat);
            }
            else
            {
                // If no specific location, get all locations
                var allLocationStats = await _contactServiceClient.GetAllLocationStatisticsAsync(cancellationToken);
                locationStatistics.AddRange(allLocationStats);
            }

            _logger.LogInformation("Generated {Count} location statistics", locationStatistics.Count);
            return locationStatistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating location statistics for location: {Location}", location);
            throw;
        }
    }

    private async Task<ReportFileData> GenerateReportFileAsync(Report report, CancellationToken cancellationToken)
    {
        try
        {
            var reportJson = JsonSerializer.Serialize(new
            {
                ReportId = report.Id,
                Location = report.Location,
                RequestedAt = report.RequestedAt,
                CompletedAt = report.CompletedAt,
                ProcessingDuration = report.ProcessingDuration?.ToString(@"hh\:mm\:ss"),
                Summary = new
                {
                    TotalPersonCount = report.TotalPersonCount,
                    TotalPhoneNumberCount = report.TotalPhoneNumberCount,
                    LocationCount = report.LocationStatistics.Count
                },
                LocationStatistics = report.LocationStatistics.Select(ls => new
                {
                    Location = ls.Location,
                    PersonCount = ls.PersonCount,
                    PhoneNumberCount = ls.PhoneNumberCount
                }).ToList()
            }, new JsonSerializerOptions { WriteIndented = true });

            // In a real implementation, save to file system or cloud storage
            // For now, we'll simulate file creation
            var fileName = $"report_{report.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine("reports", fileName);

            // Ensure directory exists
            Directory.CreateDirectory("reports");
            await File.WriteAllTextAsync(filePath, reportJson, cancellationToken);

            var fileInfo = new FileInfo(filePath);

            return new ReportFileData
            {
                FilePath = filePath,
                Format = "JSON",
                SizeBytes = fileInfo.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report file for Report ID: {ReportId}", report.Id);
            throw;
        }
    }

    private async Task PublishReportCompletedEventAsync(Report report, CancellationToken cancellationToken)
    {
        try
        {
            var completedEvent = new ReportCompletedEvent
            {
                ReportId = report.Id,
                Location = report.Location,
                Status = report.Status,
                RequestedAt = report.RequestedAt,
                CompletedAt = report.CompletedAt ?? DateTime.UtcNow,
                ProcessingTimeSeconds = report.ProcessingDuration?.TotalSeconds ?? 0,
                Summary = new ReportSummary
                {
                    TotalPersons = report.TotalPersonCount,
                    TotalPhoneNumbers = report.TotalPhoneNumberCount,
                    LocationCount = report.LocationStatistics.Count,
                    FileSize = report.FileSizeBytes?.ToString(),
                    Format = report.FileFormat
                },
                ErrorMessage = report.ErrorMessage,
                Metadata = new Dictionary<string, object>
                {
                    ["requestedBy"] = report.RequestedBy,
                    ["filePath"] = report.FilePath ?? string.Empty
                }
            };

            await _eventPublisher.PublishAsync("report-events", completedEvent, cancellationToken);
            _logger.LogInformation("Published ReportCompleted event for Report ID: {ReportId}", report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing ReportCompleted event for Report ID: {ReportId}", report.Id);
        }
    }
}

public class ReportFileData
{
    public string FilePath { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
