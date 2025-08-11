using MediatR;
using ReportService.ApiContract.Response.Commands;
using Shared.CrossCutting.Interfaces;

namespace ReportService.ApiContract.Request.Commands;

public class CreateReportCommand : ICommand<CreateReportResponse>
{
    public string Location { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = "System";
}