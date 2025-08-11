using System.ComponentModel.DataAnnotations;

namespace ReportService.ApiContract.Contracts;

public class CreateReportDto
{
    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string RequestedBy { get; set; } = "System";
}