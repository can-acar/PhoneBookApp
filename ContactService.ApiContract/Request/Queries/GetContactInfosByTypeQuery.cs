using ContactService.ApiContract.Contracts;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Queries;

public class GetContactInfosByTypeQuery : IQuery<ApiResponse<List<ContactInfoDto>>>
{
    public Guid ContactId { get; set; }
    public int InfoType { get; set; }
}