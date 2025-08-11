using System.ComponentModel.DataAnnotations;

namespace NotificationService.ApiContract.Contracts.External;

/// <summary>
/// Contact Service GraphQL query response models
/// </summary>
public class NotificationContactsByLocationResponse
{
    public List<NotificationContactDto> GetContactsByLocation { get; set; } = new();
}

public class NotificationAllContactsResponse
{
    public List<NotificationContactDto> GetContacts { get; set; } = new();
}

public class NotificationContactDto
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(300)]
    public string Company { get; set; } = string.Empty;
}
