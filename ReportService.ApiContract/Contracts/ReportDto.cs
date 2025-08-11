namespace ReportService.ApiContract.Contracts;

public class ReportDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty; // Preparing, InProgress, Completed, Failed
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<LocationStatisticDto> LocationStatistics { get; set; } = new();
}