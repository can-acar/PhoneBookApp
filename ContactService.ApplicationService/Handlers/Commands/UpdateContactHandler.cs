using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Response.Commands;
using ContactService.Domain;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using MediatR;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApplicationService.Handlers.Commands;

public class UpdateContactHandler : ICommandHandler<UpdateContactCommand, Guid>
{
    private readonly IContactService _contactService;

    public UpdateContactHandler(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<ApiResponse<Guid>> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Contact info list oluştur
            var contactInfos = request.ContactInfos.Select(ci => new ContactInfo(
                Guid.Empty, // ContactId will be set by the service
                (ContactInfoType)ci.InfoType,
                ci.InfoValue
            ));

            var updatedContact = await _contactService.UpdateContactAsync(
                request.Id,
                request.FirstName,
                request.LastName,
                request.Company,
                contactInfos,
                cancellationToken);

            if (updatedContact is null)
            {
                return ApiResponse.Result(
                    false, request.Id,
                    AppMessage.UpdateContactFailed,
                    AppMessage.UpdateContactFailed.GetMessage());
            }

            return ApiResponse.Result(
                true, updatedContact.Id,
                AppMessage.ContactUpdatedSuccessfully,
                AppMessage.ContactUpdatedSuccessfully.GetMessage());
        }
        catch (Exception ex)
        {
            // Log the exception (not shown here)
            return ApiResponse.Result(
                false, Guid.Empty,
                AppMessage.UnexpectedError,
                AppMessage.UnexpectedError.GetMessage());
        }
    }
}