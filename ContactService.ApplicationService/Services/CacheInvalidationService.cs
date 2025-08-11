using ContactService.Domain.Events;
using ContactService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContactService.ApplicationService.Services;

public class CacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    // Cache key patterns
    private const string CONTACT_BY_ID_KEY = "contact:by-id:{0}";
    private const string CONTACTS_ALL_KEY = "contacts:all";
    private const string CONTACTS_BY_LOCATION_KEY = "contacts:by-location:*";
    private const string LOCATION_STATISTICS_KEY = "location-statistics";
    private const string CONTACT_PATTERN = "contact:*";

    public CacheInvalidationService(
        ICacheService cacheService,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task HandleContactCreatedAsync(ContactCreatedEvent contactCreatedEvent)
    {
        try
        {
            await InvalidateContactAggregatesAsync();
            _logger.LogDebug("Cache invalidated for contact created: {ContactId}", contactCreatedEvent.ContactId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for contact created: {ContactId}", contactCreatedEvent.ContactId);
        }
    }

    public async Task HandleContactUpdatedAsync(ContactUpdatedEvent contactUpdatedEvent)
    {
        try
        {
            // Remove specific contact
            await _cacheService.RemoveAsync(string.Format(CONTACT_BY_ID_KEY, contactUpdatedEvent.ContactId));
            
            // Invalidate aggregates
            await InvalidateContactAggregatesAsync();
            
            _logger.LogDebug("Cache invalidated for contact updated: {ContactId}", contactUpdatedEvent.ContactId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for contact updated: {ContactId}", contactUpdatedEvent.ContactId);
        }
    }

    public async Task HandleContactDeletedAsync(ContactDeletedEvent contactDeletedEvent)
    {
        try
        {
            // Remove specific contact
            await _cacheService.RemoveAsync(string.Format(CONTACT_BY_ID_KEY, contactDeletedEvent.ContactId));
            
            // Invalidate aggregates
            await InvalidateContactAggregatesAsync();
            
            _logger.LogDebug("Cache invalidated for contact deleted: {ContactId}", contactDeletedEvent.ContactId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for contact deleted: {ContactId}", contactDeletedEvent.ContactId);
        }
    }

    private async Task InvalidateContactAggregatesAsync()
    {
        var invalidationTasks = new List<Task>
        {
            _cacheService.RemoveAsync(CONTACTS_ALL_KEY),
            _cacheService.RemoveByPatternAsync(CONTACTS_BY_LOCATION_KEY),
            _cacheService.RemoveAsync(LOCATION_STATISTICS_KEY)
        };

        await Task.WhenAll(invalidationTasks);
    }
}