namespace ReportService.Domain.Interfaces;

public class ReportGenerationResult
{
    public bool Success { get; set; }
    public int TotalPersonCount { get; set; }
    public int TotalPhoneNumberCount { get; set; }
    public List<LocationStatisticData> LocationStatistics { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
}