using ContactService.ApiContract.Contracts;

namespace ContactService.ApiContract.Response.Queries;

public class GetAllReportsResponse
{
    public List<object> Reports { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
