using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Commands;

public class DeleteContactCommand : ICommand<bool>
{
    public Guid Id { get; set; }
}