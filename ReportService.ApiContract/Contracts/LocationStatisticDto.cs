namespace ReportService.ApiContract.Contracts;

public class LocationStatisticDto
{
    public string Location { get; set; } = string.Empty;
    public int PersonCount { get; set; }
    public int PhoneNumberCount { get; set; }
}