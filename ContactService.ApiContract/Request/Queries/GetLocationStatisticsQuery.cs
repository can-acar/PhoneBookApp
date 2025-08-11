using ContactService.ApiContract.Response.Queries;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Queries
{
    public class GetLocationStatisticsQuery : IQuery<List<LocationStatisticsResponse>>
    {
    }
}
