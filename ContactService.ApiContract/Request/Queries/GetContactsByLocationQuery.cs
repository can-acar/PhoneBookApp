using ContactService.ApiContract.Contracts;
using ContactService.ApiContract.Response.Queries;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Queries;

public class GetContactsByLocationQuery : IQuery<PageResponse<ContactDto>>
{
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;
}