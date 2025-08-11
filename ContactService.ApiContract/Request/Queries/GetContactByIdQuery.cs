using ContactService.ApiContract.Contracts;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Queries;

public class GetContactByIdQuery : IQuery<ContactDto>
{
    public Guid Id { get; set; }
}