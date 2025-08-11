namespace ContactService.Domain.Models;

public class LocationStatistic
{
    public string Location { get; set; } = string.Empty;
    public int ContactCount { get; set; }
    public int PhoneNumberCount { get; set; }
}