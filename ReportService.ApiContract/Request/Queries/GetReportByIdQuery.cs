using MediatR;
using ReportService.ApiContract.Contracts;
using Shared.CrossCutting.Interfaces;

namespace ReportService.ApiContract.Request.Queries;

public class GetReportByIdQuery : IQuery<ReportDto>
{
    public Guid Id { get; set; }
}