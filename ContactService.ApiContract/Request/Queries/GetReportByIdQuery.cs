using MediatR;
using ContactService.ApiContract.Contracts;

namespace ContactService.ApiContract.Request.Queries;

public class GetReportByIdQuery : IRequest<ReportDto?>
{
    public Guid ReportId { get; set; }

    public GetReportByIdQuery(Guid reportId)
    {
        ReportId = reportId;
    }
}
