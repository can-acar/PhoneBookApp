namespace ReportService.Domain.Entities;

public class ContactSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public List<ContactInfo> ContactInfos { get; set; } = new();
}

public class ContactInfo
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
