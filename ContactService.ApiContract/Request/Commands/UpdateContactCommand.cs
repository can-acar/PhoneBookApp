using ContactService.ApiContract.Contracts;
using ContactService.ApiContract.Response.Commands;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Commands
{
    public class UpdateContactCommand : ICommand<Guid>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public List<UpdateContactInfoDto> ContactInfos { get; set; } = new();
    }
}