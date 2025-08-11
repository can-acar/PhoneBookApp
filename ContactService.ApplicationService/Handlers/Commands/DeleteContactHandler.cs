using ContactService.ApiContract.Request.Commands;
using ContactService.Domain.Interfaces;
using ContactService.Domain;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Models;
using Shared.CrossCutting.Interfaces;

namespace ContactService.ApplicationService.Handlers.Commands;

public class DeleteContactHandler : ICommandHandler<DeleteContactCommand,bool>
{
    private readonly IContactService _contactService;

    public DeleteContactHandler(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contact = await _contactService.GetContactByIdAsync(request.Id, cancellationToken);
            
            if (contact == null)
            {
                return ApiResponse.Result(false,false,AppMessage.ContactNotFound,AppMessage.ContactNotFound.GetMessage());
            }

            var result = await _contactService.DeleteContactAsync(request.Id, cancellationToken);
            
            return result ? result : ApiResponse.Result(false,false,AppMessage.NotFoundData,AppMessage.NotFoundData.GetMessage());
        }
        catch (Exception ex)
        {
            return ApiResponse.Result(false, false, AppMessage.UnexpectedError, AppMessage.UnexpectedError.GetMessage());
        }
    }
}