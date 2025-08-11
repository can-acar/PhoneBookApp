using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Models;
using ContactService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContactService.Infrastructure.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly ContactDbContext _context;

        public ContactRepository(ContactDbContext context)
        {
            _context = context;
        }

        public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Contacts
                .Where(p => p.Id == id)
                .Include(c => c.ContactInfos)
                .FirstOrDefaultAsync(cancellationToken);
        }


        public async Task<(IEnumerable<Contact> contacts, int totalCount)> GetAllPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Contacts.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearchTerm = searchTerm.ToLowerInvariant();
                query = query.Where(c => c.FirstName.ToLower().Contains(normalizedSearchTerm) ||
                                         c.LastName.ToLower().Contains(normalizedSearchTerm) ||
                                         c.Company.ToLower().Contains(normalizedSearchTerm));


                // Include ContactInfos for each contact
                query = query.Include(c => c.ContactInfos);
                // Search by location if provided


                query = query.Where(c => c.ContactInfos.Any(ci => ci.InfoType == ContactInfoType.Location &&
                                                                  ci.Content.ToLower().Contains(normalizedSearchTerm)));


                // Search by phone number if provided

                query = query.Where(c => c.ContactInfos.Any(ci => ci.InfoType == ContactInfoType.PhoneNumber &&
                                                                  ci.Content.ToLower().Contains(normalizedSearchTerm)));


                query = query.Where(c => c.ContactInfos.Any(ci => ci.InfoType == ContactInfoType.EmailAddress &&
                                                                  ci.Content.ToLower().Contains(normalizedSearchTerm)));
            }

            // Apply

            var totalCount = await query.CountAsync(cancellationToken);
            var contacts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (contacts, totalCount);
        }

        public async  Task<(IEnumerable<Contact> contacts, int totalCount)> SearchAsync(int page, int pageSize, string searchTerm, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllPagedAsync(page, pageSize, cancellationToken: cancellationToken);
            }

            var normalizedSearchTerm = searchTerm.ToLowerInvariant();
            return  await GetAllPagedAsync(page, pageSize, normalizedSearchTerm, cancellationToken);
        }

        public async Task<(IEnumerable<Contact> contacts, int totalCount)> GetByLocationAsync(int page, int pageSize, string location, CancellationToken cancellationToken = default)
        {
            var normalizedSearchTerm = location.ToLowerInvariant();
            var query = _context.Contacts
                .Include(c => c.ContactInfos)
                .Where(c => c.ContactInfos.Any(ci => ci.InfoType == ContactInfoType.Location &&
                                                     ci.Content.ToLower().Contains(normalizedSearchTerm)));

            var totalCount = await query.CountAsync(cancellationToken);
            var contacts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (contacts, totalCount);
        }

       

        public async Task<Contact?> CreateAsync(Contact? contact, CancellationToken cancellationToken = default)
        {
            // Entity already has its ID set in constructor
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync(cancellationToken);
            return contact;
        }

        public async Task<Contact?> UpdateAsync(Contact? contact, CancellationToken cancellationToken = default)
        {
            _context.Contacts.Update(contact);
            await _context.SaveChangesAsync(cancellationToken);
            return contact;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var contact = await _context.Contacts.FindAsync([id], cancellationToken);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }

        public async Task<ContactInfo> AddContactInfoAsync(ContactInfo contactInfo, CancellationToken cancellationToken = default)
        {
            // Entity already has its ID set in constructor
            _context.ContactInfos.Add(contactInfo);
            await _context.SaveChangesAsync(cancellationToken);
            return contactInfo;
        }

        public async Task<bool> RemoveContactInfoAsync(Guid contactInfoId, CancellationToken cancellationToken = default)
        {
            var contactInfo = await _context.ContactInfos.FindAsync([contactInfoId], cancellationToken);
            if (contactInfo != null)
            {
                _context.ContactInfos.Remove(contactInfo);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            
            return false;
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Contacts.AnyAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<LocationStatistic>> GetLocationStatisticsAsync(CancellationToken cancellationToken = default)
        {
            // Group contacts by location and count them
            var statistics = await _context.ContactInfos
                .Where(ci => ci.InfoType == ContactInfoType.Location)
                .GroupBy(ci => ci.Content)
                .Select(g => new LocationStatistic
                {
                    Location = g.Key,
                    ContactCount = g.Count(),
                    PhoneNumberCount = _context.ContactInfos
                        .Count(ci => ci.InfoType == ContactInfoType.PhoneNumber &&
                                     _context.ContactInfos
                                         .Where(l => l.InfoType == ContactInfoType.Location && l.Content == g.Key)
                                         .Select(l => l.ContactId)
                                         .Contains(ci.ContactId))
                })
                .ToListAsync(cancellationToken);

            return statistics;
        }

       

        // DataLoader batch methods
        public async Task<IEnumerable<Contact>> GetContactsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            if (!ids.Any())
                return Enumerable.Empty<Contact>();

            return await _context.Contacts
                .Include(c => c.ContactInfos)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Contact>> GetContactsByLocationsAsync(IEnumerable<string> locations, CancellationToken cancellationToken = default)
        {
            if (!locations.Any())
                return Enumerable.Empty<Contact>();

            var normalizedLocations = locations.Select(l => l.ToLowerInvariant()).ToList();

            return await _context.Contacts
                .Include(c => c.ContactInfos)
                .Where(c => c.ContactInfos.Any(ci => ci.InfoType == ContactInfoType.Location &&
                                                     normalizedLocations.Contains(ci.Content.ToLower())))
                .ToListAsync(cancellationToken);
        }
    }
}