using ContactService.ApiContract.Contracts;
using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Response.Commands;
using ContactService.Domain.Interfaces;
using ContactService.Domain;
using MediatR;
using Shared.CrossCutting.Models;
using Shared.CrossCutting.Interfaces;
using System.Linq;
using ContactService.Domain.Entities;
using Shared.CrossCutting.Extensions;

namespace ContactService.ApplicationService.Handlers.Commands;

public class AddContactInfoHandler : ICommandHandler<AddContactInfoCommand, AddContactInfoResponse>
{
    private readonly IContactService _contactService;

    public AddContactInfoHandler(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<ApiResponse<AddContactInfoResponse>> Handle(AddContactInfoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contact = await _contactService.GetContactByIdAsync(request.ContactId, cancellationToken);

            if (contact == null)
            {
                return ApiResponse.Result<AddContactInfoResponse>(false, null, AppMessage.ContactNotFound, AppMessage.ContactNotFound.GetMessage());
            }

            var updatedContact = await _contactService.AddContactInfoAsync(
                request.ContactId,
                request.InfoType,
                request.InfoValue, cancellationToken);

            if (updatedContact?.ContactInfos == null)
            {
                return ApiResponse.Result<AddContactInfoResponse>(false, null, AppMessage.ContactNotFound, AppMessage.ContactNotFound.GetMessage());
            }

            // Eklenen iletişim bilgisini bulalım (en son eklenen)
            var addedContactInfo = updatedContact.ContactInfos
                .OrderByDescending(ci => ci.CreatedAt)
                .FirstOrDefault();

            if (addedContactInfo == null)
            {
                return ApiResponse.Result<AddContactInfoResponse>(false, null, AppMessage.ContactInfoNotFound, AppMessage.ContactInfoNotFound.GetMessage());
            }

            var response = new AddContactInfoResponse
            {
                ContactId = updatedContact.Id,
                ContactInfoId = addedContactInfo.Id,
                ContactInfo = new ContactInfoDto
                {
                    Id = addedContactInfo.Id,
                    InfoType = addedContactInfo.InfoType.GetKey(),
                    InfoValue = addedContactInfo.Content
                }
            };

            return ApiResponse.Result<AddContactInfoResponse>(
                true, 
                response, 
                200, 
                "Contact info added successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.Result<AddContactInfoResponse>(
                false,
                null,
                AppMessage.UnexpectedError, AppMessage.UnexpectedError.GetMessage());
        }
    }
}