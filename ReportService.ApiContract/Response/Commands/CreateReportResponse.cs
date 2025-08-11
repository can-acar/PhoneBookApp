namespace ReportService.ApiContract.Response.Commands;

public class CreateReportResponse
{
    public Guid ReportId { get; set; }
    public string Status { get; set; } = "Preparing";
    public string Message { get; set; } = "Report request received and queued for processing";
}