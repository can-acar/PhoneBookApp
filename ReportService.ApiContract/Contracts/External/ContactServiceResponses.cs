using System.ComponentModel.DataAnnotations;

namespace ReportService.ApiContract.Contracts.External;

/// <summary>
/// Contact Service GraphQL query response models
/// </summary>
public class ReportAllContactsResponse
{
    public List<ReportContactDto> GetContacts { get; set; } = new();
}

public class ReportContactDto
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(300)]
    public string Company { get; set; } = string.Empty;
}
