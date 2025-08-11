namespace ContactService.ApiContract.Contracts;

public class ContactSummaryDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    
    public List<ContactInfoDto> Contacts { get; set; } = [];
    public int ContactInfoCount { get; set; }
}