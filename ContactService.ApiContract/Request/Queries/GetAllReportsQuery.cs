using MediatR;
using ContactService.ApiContract.Response.Queries;

namespace ContactService.ApiContract.Request.Queries;

public class GetAllReportsQuery : IRequest<GetAllReportsResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
