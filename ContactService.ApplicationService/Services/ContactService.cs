using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Events;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Models;
using Microsoft.AspNetCore.Http;
using Shared.CrossCutting.Models;

namespace ContactService.ApplicationService.Services
{
    public class ContactService : IContactServiceCore
    {
        private readonly IContactRepository _contactRepository;
        private readonly IOutboxService _outboxService;
        private readonly IContactHistoryService _contactHistoryService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ContactService(
            IContactRepository contactRepository,
            IOutboxService outboxService,
            IContactHistoryService contactHistoryService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contactRepository = contactRepository;
            _outboxService = outboxService;
            _contactHistoryService = contactHistoryService;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<Contact?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Contact ID cannot be empty.", nameof(id));
            }

            return await _contactRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<Pagination<Contact>> GetAllContactsAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1)
            {
                throw new ArgumentOutOfRangeException("Page and page size must be greater than zero.");
            }

            var result = await _contactRepository.GetAllPagedAsync(page, pageSize, searchTerm, cancellationToken);

            return Pagination<Contact>.Create(result.contacts.ToList(), page, pageSize, result.totalCount);
        }

        public async Task<Pagination<Contact>> GetContactsFilterByCompany(int page, int pageSize, string? company = null, CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1)
            {
                throw new ArgumentOutOfRangeException("Page and page size must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(company))
            {
                return await GetAllContactsAsync(page, pageSize, cancellationToken: cancellationToken);
            }

            var result = await _contactRepository.GetByLocationAsync(page, pageSize, company, cancellationToken);

            return Pagination<Contact>.Create(result.contacts.ToList(), page, pageSize, result.totalCount);
        }

        public async Task<Pagination<Contact>> GetContactsFilterByLocation(int page, int pageSize, string? location = null, CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1)
            {
                throw new ArgumentOutOfRangeException("Page and page size must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                return await GetAllContactsAsync(page, pageSize, cancellationToken: cancellationToken);
            }

            var result = await _contactRepository.GetByLocationAsync(page, pageSize, location, cancellationToken);

            return Pagination<Contact>.Create(result.contacts.ToList(), page, pageSize, result.totalCount);
        }

        public async Task<Contact?> CreateContactAsync(string firstName, string lastName, string company, IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new ArgumentException("Contact name cannot be empty.", nameof(firstName));
            }

            // Parse the full name into first and last name
            var nameParts = firstName.Trim().Split(' ', 2);


            // Create new contact
            var contact = new Contact(firstName, lastName, company ?? string.Empty);

            // Add contact to repository
            await _contactRepository.CreateAsync(contact, cancellationToken);

            // Add contact information
            foreach (var info in contactInfos)
            {
                if (info != null)
                {
                    contact.AddContactInfo(info.InfoType, info.Content);
                }
            }

            // Update the contact with the contact information
            if (contactInfos.Any())
            {
                await _contactRepository.UpdateAsync(contact, cancellationToken);
            }

            // Create an event for the contact creation
            var contactCreatedEvent = new ContactCreatedEvent
            {
                ContactId = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Company = contact.Company,
                CreatedAt = contact.CreatedAt,
                CorrelationId = GetCorrelationId(),
                ContactInfos = contactInfos.Select(c => new ContactInfoEventData
                {
                    Id = c.Id,
                    InfoType = (int)c.InfoType,
                    Content = c.Content
                }).ToList()
            };

            // Add to outbox for eventual consistency
            await _outboxService.AddEventAsync(
                "ContactCreated",
                contactCreatedEvent,
                contactCreatedEvent.CorrelationId,
                cancellationToken);

            // Record contact history
            await _contactHistoryService.RecordContactHistoryAsync(
                contact.Id,
                "CREATE",
                new { contact.FirstName, contact.LastName, contact.Company, ContactInfos = contactInfos },
                contactCreatedEvent.CorrelationId,
                null,
                cancellationToken);

            return contact;
        }


        public async Task<Contact?> UpdateContactAsync(Guid id, string firstName, string lastName, string company, IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Contact ID cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new ArgumentException("Contact name cannot be empty.", nameof(firstName));
            }

            // Get existing contact
            var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);

            if (contact == null)
            {
                return null;
            }


            // Store old values for history
            var oldValues = new
            {
                contact.FirstName,
                contact.LastName,
                contact.Company,
                ContactInfos = contact.ContactInfos.ToList()
            };

            // Update basic info
            contact.UpdateContactInformation(firstName, lastName, company ?? string.Empty);

            // Update contact infos if provided
            if (contactInfos != null && contactInfos.Any())
            {
                // Remove existing contact infos that are not in the new list
                foreach (var existingInfo in contact.ContactInfos.ToList())
                {
                    var matchingInfo = contactInfos.FirstOrDefault(ci =>
                        ci.Id == existingInfo.Id ||
                        (ci.InfoType == existingInfo.InfoType && ci.Content == existingInfo.Content));

                    if (matchingInfo == null)
                    {
                        contact.RemoveContactInfo(existingInfo.Id);
                    }
                }

                // Add new contact infos
                foreach (var newInfo in contactInfos)
                {
                    var existingInfo = contact.ContactInfos.FirstOrDefault(ci =>
                        ci.Id == newInfo.Id ||
                        (ci.InfoType == newInfo.InfoType && ci.Content == newInfo.Content));

                    if (existingInfo == null)
                    {
                        contact.AddContactInfo(newInfo.InfoType, newInfo.Content);
                    }
                }
            }

            // Save changes
            await _contactRepository.UpdateAsync(contact, cancellationToken);

            // Create event
            var contactUpdatedEvent = new ContactUpdatedEvent
            {
                ContactId = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Company = contact.Company,
                UpdatedAt = DateTime.UtcNow,
                CorrelationId = GetCorrelationId(),
                ContactInfos = contact.ContactInfos.Select(c => new ContactInfoEventData
                {
                    Id = c.Id,
                    InfoType = (int)c.InfoType,
                    Content = c.Content
                }).ToList()
            };

            // Add to outbox
            await _outboxService.AddEventAsync(
                "ContactUpdated",
                contactUpdatedEvent,
                contactUpdatedEvent.CorrelationId,
                cancellationToken);

            // Record history
            await _contactHistoryService.RecordContactHistoryAsync(
                contact.Id,
                "UPDATE",
                new
                {
                    OldValues = oldValues,
                    NewValues = new { contact.FirstName, contact.LastName, contact.Company, ContactInfos = contact.ContactInfos }
                },
                contactUpdatedEvent.CorrelationId,
                null,
                cancellationToken);

            return contact;
        }

        private string GetCorrelationId()
        {
            return _httpContextAccessor?.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                   ?? Guid.NewGuid().ToString();
        }

        public async Task<Contact?> UpdateContactAsync(Guid id, string name, string company, IEnumerable<ContactInfo> contactInfos, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Contact ID cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Contact name cannot be empty.", nameof(name));
            }

            // Get existing contact
            var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);

            if (contact == null)
            {
                return null;
            }

            // Parse name
            var nameParts = name.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            // Store old values for history
            var oldValues = new
            {
                contact.FirstName,
                contact.LastName,
                contact.Company,
                ContactInfos = contact.ContactInfos.ToList()
            };

            // Update basic info
            contact.UpdateContactInformation(firstName, lastName, company ?? string.Empty);

            // Update contact infos if provided
            if (contactInfos != null && contactInfos.Any())
            {
                // Remove existing contact infos that are not in the new list
                foreach (var existingInfo in contact.ContactInfos.ToList())
                {
                    var matchingInfo = contactInfos.FirstOrDefault(ci =>
                        ci.Id == existingInfo.Id ||
                        (ci.InfoType == existingInfo.InfoType && ci.Content == existingInfo.Content));

                    if (matchingInfo == null)
                    {
                        contact.RemoveContactInfo(existingInfo.Id);
                    }
                }

                // Add new contact infos
                foreach (var newInfo in contactInfos)
                {
                    var existingInfo = contact.ContactInfos.FirstOrDefault(ci =>
                        ci.Id == newInfo.Id ||
                        (ci.InfoType == newInfo.InfoType && ci.Content == newInfo.Content));

                    if (existingInfo == null)
                    {
                        contact.AddContactInfo(newInfo.InfoType, newInfo.Content);
                    }
                }
            }

            // Save changes
            await _contactRepository.UpdateAsync(contact, cancellationToken);

            // Create event
            var contactUpdatedEvent = new ContactUpdatedEvent
            {
                ContactId = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Company = contact.Company,
                UpdatedAt = DateTime.UtcNow,
                CorrelationId = GetCorrelationId(),
                ContactInfos = contact.ContactInfos.Select(c => new ContactInfoEventData
                {
                    Id = c.Id,
                    InfoType = (int)c.InfoType,
                    Content = c.Content
                }).ToList()
            };

            // Add to outbox
            await _outboxService.AddEventAsync(
                "ContactUpdated",
                contactUpdatedEvent,
                contactUpdatedEvent.CorrelationId,
                cancellationToken);

            // Record history
            await _contactHistoryService.RecordContactHistoryAsync(
                contact.Id,
                "UPDATE",
                new
                {
                    OldValues = oldValues,
                    NewValues = new { contact.FirstName, contact.LastName, contact.Company, ContactInfos = contact.ContactInfos }
                },
                contactUpdatedEvent.CorrelationId,
                null,
                cancellationToken);

            return contact;
        }

        public async Task<bool> DeleteContactAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Contact ID cannot be empty.", nameof(id));
            }

            var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);

            if (contact == null)
            {
                return false;
            }

            // Store contact data for history and events before deletion
            var contactData = new
            {
                contact.Id,
                contact.FirstName,
                contact.LastName,
                contact.Company,
                ContactInfos = contact.ContactInfos.Select(c => new
                {
                    c.Id,
                    InfoType = (int)c.InfoType,
                    c.Content
                }).ToList()
            };

            // Delete contact
            var result = await _contactRepository.DeleteAsync(id, cancellationToken);

            if (result)
            {
                // Create event
                var contactDeletedEvent = new ContactDeletedEvent
                {
                    ContactId = id,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    Company = contact.Company,
                    DeletedAt = DateTime.UtcNow,
                    CorrelationId = GetCorrelationId()
                };

                // Add to outbox
                await _outboxService.AddEventAsync(
                    "ContactDeleted",
                    contactDeletedEvent,
                    contactDeletedEvent.CorrelationId,
                    cancellationToken);

                // Record history
                await _contactHistoryService.RecordContactHistoryAsync(
                    id,
                    "DELETE",
                    contactData,
                    contactDeletedEvent.CorrelationId,
                    null,
                    cancellationToken);
            }

            return result;
        }

        public async Task<Contact?> AddContactInfoAsync(Guid contactId, int infoType, string infoValue, CancellationToken cancellationToken = default)
        {
            if (contactId == Guid.Empty)
            {
                throw new ArgumentException("Contact ID cannot be empty.", nameof(contactId));
            }

            if (string.IsNullOrWhiteSpace(infoValue))
            {
                throw new ArgumentException("Contact info value cannot be empty.", nameof(infoValue));
            }

            var contact = await _contactRepository.GetByIdAsync(contactId, cancellationToken);

            if (contact == null)
            {
                return null;
            }

            // Convert int to enum
            var infoTypeEnum = (ContactInfoType)infoType;

            // Add the contact info
            contact.AddContactInfo(infoTypeEnum, infoValue);
            var contactInfo = contact.ContactInfos.Last();

            // Save changes
            await _contactRepository.UpdateAsync(contact, cancellationToken);

            // Create event
            var contactInfoAddedEvent = new ContactInfoAddedEvent
            {
                ContactId = contactId,
                ContactInfoId = contactInfo.Id,
                InfoType = infoType,
                Content = infoValue,
                AddedAt = DateTime.UtcNow,
                CorrelationId = GetCorrelationId()
            };

            // Add to outbox
            await _outboxService.AddEventAsync(
                "ContactInfoAdded",
                contactInfoAddedEvent,
                contactInfoAddedEvent.CorrelationId,
                cancellationToken);

            // Record history
            await _contactHistoryService.RecordContactHistoryAsync(
                contactId,
                "ADD_CONTACT_INFO",
                new { ContactInfo = new { contactInfo.Id, InfoType = infoType, Content = infoValue } },
                contactInfoAddedEvent.CorrelationId,
                null,
                cancellationToken);

            return contact;
        }

        public async Task<bool> RemoveContactInfoAsync(Guid contactId, Guid contactInfoId, CancellationToken cancellationToken = default)
        {
            if (contactId == Guid.Empty)
            {
                throw new ArgumentException("Contact ID cannot be empty.", nameof(contactId));
            }

            if (contactInfoId == Guid.Empty)
            {
                throw new ArgumentException("Contact Info ID cannot be empty.", nameof(contactInfoId));
            }

            return await _contactRepository.RemoveContactInfoAsync(contactInfoId, cancellationToken);
        }

        public async Task<bool> ContactExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Contact ID cannot be empty.", nameof(id));
            }

            return await _contactRepository.ExistsAsync(id, cancellationToken);
        }

        public async Task<List<LocationStatistic>> GetLocationStatistics(CancellationToken cancellationToken)
        {
            var statistics = await _contactRepository.GetLocationStatisticsAsync(cancellationToken);

            if (statistics == null || !statistics.Any())
            {
                return new List<LocationStatistic>();
            }

            // Create a list of LocationStatistic objects
            return statistics.ToList();
        }

        public async Task<bool> AddContactInfoAsync(Guid contactId, ContactInfoType infoType, string content, CancellationToken cancellationToken = default)
        {
            if (contactId == Guid.Empty)
            {
                throw new ArgumentException("Contact ID cannot be empty.", nameof(contactId));
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Contact info content cannot be empty.", nameof(content));
            }

            var contact = await _contactRepository.GetByIdAsync(contactId, cancellationToken);

            if (contact == null)
            {
                return false;
            }

            // Add new contact info
            contact.AddContactInfo(infoType, content);
            var contactInfo = contact.ContactInfos.Last();

            // Save changes
            await _contactRepository.UpdateAsync(contact, cancellationToken);

            // Create event
            var contactInfoAddedEvent = new ContactInfoAddedEvent
            {
                ContactId = contactId,
                ContactInfoId = contactInfo.Id,
                InfoType = (int)infoType,
                Content = content,
                AddedAt = DateTime.UtcNow,
                CorrelationId = GetCorrelationId()
            };

            // Add to outbox
            await _outboxService.AddEventAsync(
                "ContactInfoAdded",
                contactInfoAddedEvent,
                contactInfoAddedEvent.CorrelationId,
                cancellationToken);

            // Record history
            await _contactHistoryService.RecordContactHistoryAsync(
                contactId,
                "ADD_CONTACT_INFO",
                new { ContactInfo = new { contactInfo.Id, InfoType = (int)contactInfo.InfoType, contactInfo.Content } },
                contactInfoAddedEvent.CorrelationId,
                null,
                cancellationToken);

            return true;
        }
    }
}