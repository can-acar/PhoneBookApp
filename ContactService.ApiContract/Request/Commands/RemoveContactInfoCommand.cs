using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Commands;

public class RemoveContactInfoCommand : ICommand
{
    public Guid ContactId { get; set; }
    public Guid ContactInfoId { get; set; }
}