using System.ComponentModel.DataAnnotations;

namespace ContactService.ApiContract.Contracts;

public class ReportDto
{
    public string Id { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ContactCount { get; set; }
    public int PhoneNumberCount { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public LocationStatisticsDto? LocationStatistics { get; set; }
}

public class LocationStatisticsDto
{
    public string Location { get; set; } = string.Empty;
    public int ContactCount { get; set; }
    public int PhoneNumberCount { get; set; }
}
