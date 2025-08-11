namespace ReportService.Domain.Interfaces;

public class ContactInfoSummary
{
    public Guid Id { get; set; }
    public int InfoType { get; set; } // 1=Phone, 2=Email, 3=Location
    public string InfoValue { get; set; } = string.Empty;
}