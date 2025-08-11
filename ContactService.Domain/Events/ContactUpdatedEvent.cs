namespace ContactService.Domain.Events;

public class ContactUpdatedEvent
{
    public Guid ContactId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public List<ContactInfoEventData> ContactInfos { get; set; } = new();
    public ContactChangeData Changes { get; set; } = new();
}

public class ContactChangeData
{
    public string? OldFirstName { get; set; }
    public string? OldLastName { get; set; }
    public string? OldCompany { get; set; }
    public string? NewFirstName { get; set; }
    public string? NewLastName { get; set; }
    public string? NewCompany { get; set; }
}