namespace ReportService.Domain.Interfaces;

public class LocationStatisticData
{
    public string Location { get; set; } = string.Empty;
    public int PersonCount { get; set; }
    public int PhoneNumberCount { get; set; }
}