using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Models;
using Microsoft.Extensions.Logging;
using ContactService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Shared.CrossCutting.Models; // For Pagination<T>

namespace ContactService.ApplicationService.Services
{
    public class CachedContactService : IContactService, ICachedContactService
    {
        private readonly IContactServiceCore _baseContactService;
        private readonly ICacheService _cacheService;
        private readonly RedisSettings _redisSettings;
        private readonly ILogger<CachedContactService> _logger;

        // Cache key patterns
        private const string CONTACT_BY_ID_KEY = "contact:by-id:{0}";
        private const string CONTACTS_PAGED_KEY = "contacts:paged:page={0}:size={1}:q={2}";
        private const string CONTACTS_BY_COMPANY_KEY = "contacts:by-company:page={0}:size={1}:company={2}";
        private const string CONTACTS_BY_LOCATION_KEY = "contacts:by-location:page={0}:size={1}:location={2}";
        private const string LOCATION_STATISTICS_KEY = "location-statistics";
        private const string CONTACT_PATTERN = "contact:*";
        private const string CONTACTS_PATTERN = "contacts:*";

        public CachedContactService(
            IContactServiceCore baseContactService,
            ICacheService cacheService,
            IOptions<RedisSettings> redisSettings,
            ILogger<CachedContactService> logger)
        {
            _baseContactService = baseContactService;
            _cacheService = cacheService;
            _redisSettings = redisSettings.Value;
            _logger = logger;
        }

        public async Task<Contact?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CONTACT_BY_ID_KEY, id);

            var cachedContact = await _cacheService.GetAsync<Contact>(cacheKey, cancellationToken);
            if (cachedContact != null)
            {
                _logger.LogDebug("Retrieved contact {ContactId} from cache", id);
                return cachedContact;
            }

            var contact = await _baseContactService.GetContactByIdAsync(id, cancellationToken);
            if (contact != null)
            {
                await _cacheService.SetAsync(cacheKey, contact, _redisSettings.ContactCacheExpiration, cancellationToken);
                _logger.LogDebug("Retrieved contact {ContactId} from database and cached", id);
            }

            return contact;
        }

        // NOTE: Signature adjusted to match interface
        public async Task<Pagination<Contact>> GetAllContactsAsync(
            int page,
            int pageSize,
            string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var safeQuery = SanitizeForKey(searchTerm);
            var cacheKey = string.Format(CONTACTS_PAGED_KEY, page, pageSize, safeQuery);

            var cached = await _cacheService.GetAsync<Pagination<Contact>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved paged contacts from cache: page={Page}, size={Size}, q={Query}", page, pageSize, safeQuery);
                return cached;
            }

            var result = await _baseContactService.GetAllContactsAsync(page, pageSize, searchTerm, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, _redisSettings.ContactCacheExpiration, cancellationToken);
            _logger.LogDebug("Retrieved paged contacts from database and cached: page={Page}, size={Size}, q={Query}", page, pageSize, safeQuery);

            return result;
        }

        public async Task<Pagination<Contact>> GetContactsFilterByCompany(
            int page,
            int pageSize,
            string? company = null,
            CancellationToken cancellationToken = default)
        {
            var safeCompany = SanitizeForKey(company);
            var cacheKey = string.Format(CONTACTS_BY_COMPANY_KEY, page, pageSize, safeCompany);

            var cached = await _cacheService.GetAsync<Pagination<Contact>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved contacts by company from cache: page={Page}, size={Size}, company={Company}", page, pageSize, safeCompany);
                return cached;
            }

            var result = await _baseContactService.GetContactsFilterByCompany(page, pageSize, company, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, _redisSettings.ContactCacheExpiration, cancellationToken);
            _logger.LogDebug("Retrieved contacts by company from database and cached: page={Page}, size={Size}, company={Company}", page, pageSize, safeCompany);

            return result;
        }

        public async Task<Pagination<Contact>> GetContactsFilterByLocation(
            int page,
            int pageSize,
            string? location = null,
            CancellationToken cancellationToken = default)
        {
            var safeLocation = SanitizeForKey(location);
            var cacheKey = string.Format(CONTACTS_BY_LOCATION_KEY, page, pageSize, safeLocation);

            var cached = await _cacheService.GetAsync<Pagination<Contact>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved contacts by location from cache: page={Page}, size={Size}, location={Location}", page, pageSize, safeLocation);
                return cached;
            }

            var result = await _baseContactService.GetContactsFilterByLocation(page, pageSize, location, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, _redisSettings.ContactCacheExpiration, cancellationToken);
            _logger.LogDebug("Retrieved contacts by location from database and cached: page={Page}, size={Size}, location={Location}", page, pageSize, safeLocation);

            return result;
        }

        public async Task<Contact?> CreateContactAsync(
            string firstName,
            string lastName,
            string company,
            IEnumerable<ContactInfo> contactInfos,
            CancellationToken cancellationToken = default)
        {
            var created = await _baseContactService.CreateContactAsync(firstName, lastName, company, contactInfos, cancellationToken);

            await InvalidateContactCachesAsync(cancellationToken);
            if (created != null)
            {
                _logger.LogDebug("Created contact {ContactId} and invalidated related caches", created.Id);
            }
            else
            {
                _logger.LogDebug("CreateContactAsync returned null; invalidated related caches");
            }

            return created;
        }

        public async Task<Contact?> UpdateContactAsync(
            Guid id,
            string firstName,
            string lastName,
            string company,
            IEnumerable<ContactInfo> contactInfos,
            CancellationToken cancellationToken = default)
        {
            var updated = await _baseContactService.UpdateContactAsync(id, firstName, lastName, company, contactInfos, cancellationToken);

            await _cacheService.RemoveAsync(string.Format(CONTACT_BY_ID_KEY, id), cancellationToken);
            await InvalidateContactCachesAsync(cancellationToken);

            _logger.LogDebug("Updated contact {ContactId} and invalidated related caches", id);
            return updated;
        }

        public async Task<bool> DeleteContactAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _baseContactService.DeleteContactAsync(id, cancellationToken);

            await _cacheService.RemoveAsync(string.Format(CONTACT_BY_ID_KEY, id), cancellationToken);
            await InvalidateContactCachesAsync(cancellationToken);

            _logger.LogDebug("Deleted contact {ContactId} and invalidated related caches", id);
            return result;
        }

        public async Task<Contact?> AddContactInfoAsync(
            Guid contactId,
            int infoType,
            string infoValue,
            CancellationToken cancellationToken = default)
        {
            var updated = await _baseContactService.AddContactInfoAsync(contactId, infoType, infoValue, cancellationToken);

            await _cacheService.RemoveAsync(string.Format(CONTACT_BY_ID_KEY, contactId), cancellationToken);
            await InvalidateContactCachesAsync(cancellationToken);

            _logger.LogDebug("Added contact info for contact {ContactId} and invalidated related caches", contactId);
            return updated;
        }

        public async Task<bool> RemoveContactInfoAsync(
            Guid contactId,
            Guid contactInfoId,
            CancellationToken cancellationToken = default)
        {
            var result = await _baseContactService.RemoveContactInfoAsync(contactId, contactInfoId, cancellationToken);

            await _cacheService.RemoveAsync(string.Format(CONTACT_BY_ID_KEY, contactId), cancellationToken);
            await InvalidateContactCachesAsync(cancellationToken);

            _logger.LogDebug("Removed contact info for contact {ContactId} and invalidated related caches", contactId);
            return result;
        }

        public async Task<bool> ContactExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CONTACT_BY_ID_KEY, id);
            var cachedContact = await _cacheService.GetAsync<Contact>(cacheKey, cancellationToken);

            if (cachedContact != null)
            {
                _logger.LogDebug("Found contact {ContactId} in cache", id);
                return true;
            }

            return await _baseContactService.ContactExistsAsync(id, cancellationToken);
        }

        // NOTE: Method name/signature aligned with interface (no Async suffix, List<> return type)
        public async Task<List<LocationStatistic>> GetLocationStatistics(CancellationToken cancellationToken)
        {
            var cached = await _cacheService.GetAsync<List<LocationStatistic>>(LOCATION_STATISTICS_KEY, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Retrieved location statistics from cache");
                return cached;
            }

            var stats = await _baseContactService.GetLocationStatistics(cancellationToken);
            await _cacheService.SetAsync(LOCATION_STATISTICS_KEY, stats, _redisSettings.LocationStatsCacheExpiration, cancellationToken);

            _logger.LogDebug("Retrieved location statistics from database and cached");
            return stats;
        }

        private async Task InvalidateContactCachesAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Broad invalidation for any contact* or contacts* keys
                await _cacheService.RemoveByPatternAsync(CONTACT_PATTERN, cancellationToken);
                await _cacheService.RemoveByPatternAsync(CONTACTS_PATTERN, cancellationToken);

                // Aggregate caches
                await _cacheService.RemoveAsync(LOCATION_STATISTICS_KEY, cancellationToken);

                _logger.LogDebug("Invalidated contact-related caches");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating contact caches, continuing with operation");
            }
        }

        private static string SanitizeForKey(string? input)
            => (input ?? string.Empty).Trim().ToLowerInvariant();
    }
}