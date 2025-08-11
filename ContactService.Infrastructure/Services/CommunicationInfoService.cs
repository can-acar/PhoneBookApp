using ContactService.Domain.Entities;
using ContactService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContactService.Infrastructure.Services;

/// <summary>
/// Service for handling communication/contact information operations
/// Optimized for DataLoader batch operations
/// </summary>
public class CommunicationInfoService
{
    private readonly ContactDbContext _context;
    private readonly ILogger<CommunicationInfoService> _logger;

    public CommunicationInfoService(
        ContactDbContext context,
        ILogger<CommunicationInfoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Batch load contact information for multiple contact IDs
    /// Used by ContactInfoByContactIdDataLoader to prevent N+1 queries
    /// </summary>
    public async Task<IEnumerable<ContactInfo>> GetContactInfosByContactIdsAsync(
        IEnumerable<Guid> contactIds,
        CancellationToken cancellationToken = default)
    {
        if (!contactIds.Any())
        {
            return Enumerable.Empty<ContactInfo>();
        }

        var contactIdsList = contactIds.ToList();

        _logger.LogDebug(
            "Batch loading contact information for {ContactCount} contacts: {ContactIds}",
            contactIdsList.Count, contactIdsList);

        try
        {
            var contactInfos = await _context.ContactInfos
                .Where(ci => contactIdsList.Contains(ci.ContactId))
                .OrderBy(ci => ci.ContactId)
                .ThenBy(ci => ci.InfoType)
                .ToListAsync(cancellationToken);

            _logger.LogDebug(
                "Successfully loaded {InfoCount} contact information records for {ContactCount} contacts",
                contactInfos.Count, contactIdsList.Count);

            return contactInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to batch load contact information for contacts: {ContactIds}",
                contactIdsList);
            throw;
        }
    }

    /// <summary>
    /// Batch load contacts by multiple locations
    /// Used by ContactsByLocationDataLoader to prevent N+1 queries
    /// </summary>
    public async Task<IEnumerable<Contact>> GetContactsByLocationsAsync(
        IEnumerable<string> locations,
        CancellationToken cancellationToken = default)
    {
        if (!locations.Any())
        {
            return Enumerable.Empty<Contact>();
        }

        var locationsList = locations.Select(l => l.ToLowerInvariant()).ToList();

        _logger.LogDebug(
            "Batch loading contacts for {LocationCount} locations: {Locations}",
            locationsList.Count, locationsList);

        try
        {
            // Get contacts that have location information matching the requested locations
            var contacts = await _context.Contacts
                .Include(c => c.ContactInfos)
                .Where(c => c.ContactInfos.Any(ci =>
                    ci.InfoType == Domain.Enums.ContactInfoType.Location &&
                    locationsList.Contains(ci.Content.ToLower())))
                .ToListAsync(cancellationToken);

            _logger.LogDebug(
                "Successfully loaded {ContactCount} contacts for {LocationCount} locations",
                contacts.Count, locationsList.Count);

            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to batch load contacts for locations: {Locations}",
                locationsList);
            throw;
        }
    }

    /// <summary>
    /// Get contact information by contact ID (single)
    /// </summary>
    public async Task<IEnumerable<ContactInfo>> GetContactInfosByContactIdAsync(
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ContactInfos
            .Where(ci => ci.ContactId == contactId)
            .OrderBy(ci => ci.InfoType)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get contact information statistics by type
    /// </summary>
    public async Task<Dictionary<Domain.Enums.ContactInfoType, int>> GetContactInfoStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var statistics = await _context.ContactInfos
            .GroupBy(ci => ci.InfoType)
            .Select(g => new { InfoType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(
                x => x.InfoType,
                x => x.Count,
                cancellationToken);

        return statistics;
    }
}