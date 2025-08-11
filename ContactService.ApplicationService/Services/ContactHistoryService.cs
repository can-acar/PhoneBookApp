using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ContactService.ApplicationService.Services
{
    public class ContactHistoryService : IContactHistoryService
    {
        private readonly IContactHistoryRepository _contactHistoryRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ContactHistoryService> _logger;

        public ContactHistoryService(
            IContactHistoryRepository contactHistoryRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ContactHistoryService> logger)
        {
            _contactHistoryRepository = contactHistoryRepository ?? throw new ArgumentNullException(nameof(contactHistoryRepository));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RecordContactHistoryAsync(
            Guid contactId,
            string operationType,
            object contactData,
            string correlationId,
            string? userId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = GetClientIpAddress(httpContext);
                var userAgent = httpContext?.Request.Headers["User-Agent"].FirstOrDefault();

                var additionalMetadata = new Dictionary<string, object>();
                
                if (httpContext != null)
                {
                    additionalMetadata["RequestPath"] = httpContext.Request.Path.ToString();
                    additionalMetadata["HttpMethod"] = httpContext.Request.Method;
                    additionalMetadata["RequestTime"] = DateTime.UtcNow;
                    
                    if (httpContext.Response.StatusCode > 0)
                    {
                        additionalMetadata["ResponseStatusCode"] = httpContext.Response.StatusCode;
                    }
                }

                var contactHistory = new ContactHistory(
                    contactId,
                    operationType,
                    contactData,
                    correlationId,
                    userId,
                    ipAddress,
                    userAgent,
                    additionalMetadata);

                await _contactHistoryRepository.CreateAsync(contactHistory, cancellationToken);

                _logger.LogInformation(
                    "Contact history recorded: ContactId={ContactId}, Operation={Operation}, CorrelationId={CorrelationId}",
                    contactId, operationType, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to record contact history: ContactId={ContactId}, Operation={Operation}, CorrelationId={CorrelationId}",
                    contactId, operationType, correlationId);
                
                // Don't rethrow - audit logging should not break business operations
                // Consider implementing a fallback mechanism or dead letter queue
            }
        }

        public async Task<ContactHistory?> GetHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default)
        {
            return await _contactHistoryRepository.GetByIdAsync(historyId, cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetContactHistoryAsync(Guid contactId, CancellationToken cancellationToken = default)
        {
            return await _contactHistoryRepository.GetByContactIdAsync(contactId, cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetHistoryByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            return await _contactHistoryRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetHistoryByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default)
        {
            return await _contactHistoryRepository.GetByOperationTypeAsync(operationType, cancellationToken);
        }

        public async Task<IEnumerable<ContactHistory>> GetHistoryByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _contactHistoryRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        }

        public async Task<Contact?> ReplayContactStateAsync(Guid contactId, DateTime? pointInTime = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var historyRecords = await _contactHistoryRepository.GetContactHistoryForReplayAsync(
                    contactId, 
                    null, // Get all history from the beginning
                    cancellationToken);

                // Filter by point in time if specified
                if (pointInTime.HasValue)
                {
                    historyRecords = historyRecords.Where(h => h.Timestamp <= pointInTime.Value);
                }

                var orderedHistory = historyRecords.OrderBy(h => h.Timestamp).ToList();

                if (!orderedHistory.Any())
                {
                    _logger.LogWarning("No history records found for contact {ContactId}", contactId);
                    return null;
                }

                Contact? replayedContact = null;

                foreach (var historyRecord in orderedHistory)
                {
                    switch (historyRecord.OperationType)
                    {
                        case ContactHistoryOperationType.CREATE:
                            var createData = historyRecord.GetContactData<ContactCreateData>();
                            if (createData != null)
                            {
                                replayedContact = new Contact(createData.FirstName, createData.LastName, createData.Company ?? string.Empty);
                                
                                // Add contact infos if available
                                if (createData.ContactInfos != null)
                                {
                                    foreach (var info in createData.ContactInfos)
                                    {
                                        replayedContact.AddContactInfo(info.InfoType, info.Content);
                                    }
                                }
                            }
                            break;

                        case ContactHistoryOperationType.UPDATE:
                            var updateData = historyRecord.GetContactData<ContactUpdateData>();
                            if (replayedContact != null && updateData != null)
                            {
                                replayedContact.UpdateContactInformation(
                                    updateData.FirstName, 
                                    updateData.LastName, 
                                    updateData.Company ?? string.Empty);

                                // Clear and re-add contact infos
                                var existingInfos = replayedContact.ContactInfosReadOnly.ToList();
                                foreach (var existingInfo in existingInfos)
                                {
                                    replayedContact.RemoveContactInfo(existingInfo.Id);
                                }

                                if (updateData.ContactInfos != null)
                                {
                                    foreach (var info in updateData.ContactInfos)
                                    {
                                        replayedContact.AddContactInfo(info.InfoType, info.Content);
                                    }
                                }
                            }
                            break;

                        case ContactHistoryOperationType.DELETE:
                            // For delete operation, we return null to indicate the contact was deleted at this point
                            replayedContact = null;
                            break;
                    }
                }

                _logger.LogInformation(
                    "Contact state replayed for ContactId={ContactId}, PointInTime={PointInTime}, FinalState={FinalState}",
                    contactId, pointInTime, replayedContact != null ? "EXISTS" : "DELETED");

                return replayedContact;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to replay contact state for ContactId={ContactId}", contactId);
                throw;
            }
        }

        public async Task<IEnumerable<ContactHistory>> GetAuditTrailAsync(Guid contactId, CancellationToken cancellationToken = default)
        {
            return await _contactHistoryRepository.GetByContactIdAsync(contactId, cancellationToken);
        }

        public async Task<int> CleanupOldHistoryAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow - retentionPeriod;
            var deletedCount = await _contactHistoryRepository.DeleteOldHistoryRecordsAsync(cutoffDate, cancellationToken);
            
            _logger.LogInformation("Cleaned up {DeletedCount} old contact history records older than {CutoffDate}", 
                deletedCount, cutoffDate);
            
            return deletedCount;
        }

        private static string? GetClientIpAddress(HttpContext? httpContext)
        {
            if (httpContext == null)
                return null;

            // Check for forwarded IP addresses (load balancer, proxy)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }

            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                return realIp;
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }

    // DTOs for history replay
    public class ContactCreateData
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Company { get; set; }
        public List<ContactInfoData>? ContactInfos { get; set; }
    }

    public class ContactUpdateData
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Company { get; set; }
        public List<ContactInfoData>? ContactInfos { get; set; }
    }

    public class ContactInfoData
    {
        public Domain.Enums.ContactInfoType InfoType { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}