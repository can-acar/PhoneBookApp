using MediatR;
using ReportService.ApiContract.Contracts;
using Shared.CrossCutting.Interfaces;

namespace ReportService.ApiContract.Request.Queries;

public class GetAllReportsQuery : IQuery<List<ReportDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
}