using System.ComponentModel.DataAnnotations;
using ContactService.Domain.Enums;

namespace ContactService.Domain.Entities;

public class ContactInfo
{
    // Private constructor for EF Core
    private ContactInfo() { }

    // Rich constructor with business rules
    public ContactInfo(Guid contactId, ContactInfoType infoType, string content)
    {
        ValidateContent(content);
        ValidateContactInfoType(infoType, content);

        Id = Guid.NewGuid();
        ContactId = contactId;
        InfoType = infoType;
        Content = content.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    
    public Guid ContactId { get; private set; }
    
    [Required]
    public ContactInfoType InfoType { get; private set; }
    
    [Required]
    [MaxLength(500)]
    public string Content { get; private set; } = string.Empty;
    
    public DateTime CreatedAt { get; private set; }
    
    // Navigation property
    public virtual Contact Contact { get; set; } = null!;

    // Business methods
    public void UpdateContent(string newContent)
    {
        ValidateContent(newContent);
        ValidateContactInfoType(InfoType, newContent);
        
        Content = newContent.Trim();
    }

    public bool IsPhoneNumber() => InfoType == ContactInfoType.PhoneNumber;
    public bool IsEmail() => InfoType == ContactInfoType.EmailAddress;
    public bool IsLocation() => InfoType == ContactInfoType.Location;

    // Private validation methods
    private static void ValidateContent(string content)
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

    private static void ValidateContactInfoType(ContactInfoType infoType, string content)
    {
        switch (infoType)
        {
            case ContactInfoType.PhoneNumber:
                ValidatePhoneNumber(content);
                break;
            case ContactInfoType.EmailAddress:
                ValidateEmail(content);
                break;
            case ContactInfoType.Location:
                ValidateLocation(content);
                break;
            default:
                throw new ArgumentException($"Invalid contact info type: {infoType}");
        }
    }

    private static void ValidatePhoneNumber(string phoneNumber)
    {
        var cleanPhone = phoneNumber.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (cleanPhone.Length < 10 || cleanPhone.Length > 15)
        {
            throw new ArgumentException("Phone number must be between 10 and 15 digits");
        }

        if (!cleanPhone.All(c => char.IsDigit(c) || c == '+'))
        {
            throw new ArgumentException("Phone number can only contain digits and + sign");
        }
    }

    private static void ValidateEmail(string email)
    {
        if (!email.Contains('@') || !email.Contains('.'))
        {
            throw new ArgumentException("Invalid email format");
        }

        var parts = email.Split('@');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new ArgumentException("Invalid email format");
        }
    }

    private static void ValidateLocation(string location)
    {
        if (location.Trim().Length < 2)
        {
            throw new ArgumentException("Location must be at least 2 characters long");
        }
    }
}