using ContactService.ApiContract.Request.Commands;
using ContactService.Domain;
using ContactService.Domain.Interfaces;
using MediatR;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApplicationService.Handlers.Commands;

public class RemoveContactInfoHandler : ICommandHandler<RemoveContactInfoCommand>
{
    private readonly IContactService _contactService;

    public RemoveContactInfoHandler(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<ApiResponse> Handle(RemoveContactInfoCommand request, CancellationToken cancellationToken)
    {
        var contact = await _contactService.GetContactByIdAsync(request.ContactId, cancellationToken);
        
        if (contact == null)
        {
            return ApiResponse.Result(false, 
                AppMessage.ContactNotFound, 
                AppMessage.ContactNotFound.GetMessage());
        }

        var result = await _contactService.RemoveContactInfoAsync(request.ContactId, request.ContactInfoId, cancellationToken);

        var message = result
            ? AppMessage.ContactInfoDeletedSuccessfully.GetMessage()
            : AppMessage.ContactInfoNotFound.GetMessage();
        
        var code = result ? AppMessage.ContactInfoDeletedSuccessfully: AppMessage.ContactInfoNotFound;
        
        return ApiResponse.Result(result,code, message);
    }
}
