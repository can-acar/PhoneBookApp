namespace ContactService.Domain.Events;

public class ContactDeletedEvent
{
    public Guid ContactId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}