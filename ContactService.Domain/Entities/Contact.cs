using System.ComponentModel.DataAnnotations;
using ContactService.Domain.Enums;

namespace ContactService.Domain.Entities;

public class Contact
{
    private readonly List<ContactInfo> _contactInfos = new();

    // Private constructor for EF Core
    private Contact() { }

    // Rich constructor with business rules
    public Contact(string firstName, string lastName, string company = "")
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));

        Id = Guid.NewGuid();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Company = company?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; private set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; private set; } = string.Empty;
    
    [MaxLength(200)]
    public string Company { get; private set; } = string.Empty;
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Encapsulated collection
    public virtual ICollection<ContactInfo> ContactInfos 
    { 
        get => _contactInfos; 
        private set => _contactInfos.AddRange(value); 
    }
    
    public IReadOnlyCollection<ContactInfo> ContactInfosReadOnly => _contactInfos.AsReadOnly();
    
    // Computed property
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Business methods
    public void UpdateContactInformation(string firstName, string lastName, string company = "")
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Company = company?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddContactInfo(ContactInfoType infoType, string content)
    {
        ValidateContactInfoContent(content);

        if (HasContactInfoOfType(infoType, content))
        {
            throw new InvalidOperationException($"Contact info of type {infoType} with content '{content}' already exists");
        }

        var contactInfo = new ContactInfo(Id, infoType, content);
        _contactInfos.Add(contactInfo);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveContactInfo(Guid contactInfoId)
    {
        var contactInfo = _contactInfos.FirstOrDefault(ci => ci.Id == contactInfoId);
        if (contactInfo == null)
        {
            throw new InvalidOperationException($"{contactInfoId} ID'li iletişim bilgisi bulunamadı");
        }

        _contactInfos.Remove(contactInfo);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasContactInfoOfType(ContactInfoType infoType)
    {
        return _contactInfos.Any(ci => ci.InfoType == infoType);
    }

    public bool HasContactInfoOfType(ContactInfoType infoType, string content)
    {
        return _contactInfos.Any(ci => ci.InfoType == infoType && 
                                     string.Equals(ci.Content, content, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<ContactInfo> GetContactInfosByType(ContactInfoType infoType)
    {
        return _contactInfos.Where(ci => ci.InfoType == infoType);
    }

    public string? GetPrimaryLocation()
    {
        return _contactInfos.FirstOrDefault(ci => ci.InfoType == ContactInfoType.Location)?.Content;
    }

    public string? GetPrimaryPhoneNumber()
    {
        return _contactInfos.FirstOrDefault(ci => ci.InfoType == ContactInfoType.PhoneNumber)?.Content;
    }

    public string? GetPrimaryEmail()
    {
        return _contactInfos.FirstOrDefault(ci => ci.InfoType == ContactInfoType.EmailAddress)?.Content;
    }

    // Private validation methods
    private static void ValidateName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
        }

        if (name.Trim().Length > 100)
        {
            throw new ArgumentException($"{paramName} cannot exceed 100 characters", paramName);
        }
    }

    private static void ValidateContactInfoContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Contact info content cannot be null or empty");
        }

        if (content.Trim().Length > 500)
        {
            throw new ArgumentException("Contact info content cannot exceed 500 characters");
        }
    }
}