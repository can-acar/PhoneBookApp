namespace NotificationService.Domain.Entities;

public class ContactSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public List<ContactInfoSummary> ContactInfos { get; set; } = new();
}

public class ContactInfoSummary
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
