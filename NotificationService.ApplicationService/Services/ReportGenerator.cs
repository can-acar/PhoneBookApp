using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Interfaces;

namespace NotificationService.ApplicationService.Services;

public class ReportGenerator : IReportGenerator
{
    private readonly IReportRepository _reportRepository;
    private readonly IContactServiceClient _contactServiceClient;

    public ReportGenerator(
        IReportRepository reportRepository,
        IContactServiceClient contactServiceClient)
    {
        _reportRepository = reportRepository;
        _contactServiceClient = contactServiceClient;
    }

    public async Task<ReportGenerationResult> GenerateLocationReportAsync(string location, CancellationToken cancellationToken = default)
    {
        try
        {
            var contacts = await _contactServiceClient.GetContactsByLocationAsync(location, cancellationToken);
            
            var locationStatistics = contacts
                .GroupBy(c => "DefaultLocation") // Placeholder - need actual location data
                .Select(g => new LocationStatisticData
                {
                    Location = g.Key,
                    PersonCount = g.Count(),
                    PhoneNumberCount = g.Sum(c => c.ContactInfos.Count)
                })
                .ToList();

            return new ReportGenerationResult
            {
                Success = true,
                TotalPersonCount = contacts.Count(),
                TotalPhoneNumberCount = contacts.Sum(c => c.ContactInfos.Count),
                LocationStatistics = locationStatistics
            };
        }
        catch (Exception ex)
        {
            return new ReportGenerationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task GenerateReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report == null) return;

        try
        {
            report.MarkAsInProgress();
            await _reportRepository.UpdateAsync(report, cancellationToken);

            // Get all contacts from ContactService
            var contacts = await _contactServiceClient.GetAllContactsAsync(cancellationToken);
            
            // Process contacts and calculate totals
            int totalPersonCount = contacts.Count();
            int totalPhoneNumberCount = contacts.Sum(c => c.ContactInfos.Count);

            // Group contacts by location and generate statistics
            var locationGroups = contacts
                .GroupBy(c => "DefaultLocation"); // Placeholder - need actual location data from contacts
                
            // Add location statistics to report
            foreach (var group in locationGroups)
            {
                string location = group.Key;
                int personCount = group.Count();
                int phoneNumberCount = group.Sum(c => c.ContactInfos.Count);
                
                report.AddLocationStatistic(location, personCount, phoneNumberCount);
            }

            // Mark report as completed with calculated totals
            report.MarkAsCompleted(totalPersonCount, totalPhoneNumberCount);
            
            await _reportRepository.UpdateAsync(report, cancellationToken);
        }
        catch (Exception ex)
        {
            report.MarkAsFailed(ex.Message);
            await _reportRepository.UpdateAsync(report, cancellationToken);
            throw;
        }
    }
}