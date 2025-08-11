namespace ContactService.Domain.Events;

public class ContactInfoAddedEvent
{
    public Guid ContactId { get; set; }
    public Guid ContactInfoId { get; set; }
    public int InfoType { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
