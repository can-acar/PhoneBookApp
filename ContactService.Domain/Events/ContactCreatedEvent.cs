namespace ContactService.Domain.Events;

public class ContactCreatedEvent
{
    public Guid ContactId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public List<ContactInfoEventData> ContactInfos { get; set; } = new();
}

public class ContactInfoEventData
{
    public Guid Id { get; set; }
    public int InfoType { get; set; }
    public string Content { get; set; } = string.Empty;
}