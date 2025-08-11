using ContactService.ApiContract.Contracts;
using ContactService.ApiContract.Request.Queries;
using ContactService.Domain.Interfaces;
using ContactService.Domain;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Extensions;
using Shared.CrossCutting.Models;
using Shared.CrossCutting.Interfaces;

namespace ContactService.ApplicationService.Handlers.Queries;

public class GetContactByIdHandler : IQueryHandler<GetContactByIdQuery, ContactDto>
{
    private readonly IContactService _contactService;

    public GetContactByIdHandler(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<ApiResponse<ContactDto>> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var contact = await _contactService.GetContactByIdAsync(request.Id, cancellationToken);

            if (contact == null)
            {
                return ApiResponse.Result<ContactDto>(
                    false,null,
                    AppMessage.ContactNotFound,
                    AppMessage.ContactNotFound.GetMessage());
            }

            var result = new ContactDto
            {
                Id = contact.Id,
                FirstName = contact.FullName,
                Company = contact.Company,
                ContactInfos = contact.ContactInfos.Select(ci => new ContactInfoDto
                {
                    Id = ci.Id,
                    InfoType = ci.InfoType.GetKey(),
                    InfoValue = ci.Content
                }).ToList(),
            };

            return ApiResponse.Result<ContactDto>(
                true, 
                result, 
                200, 
                "Contact retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.Result<ContactDto>(
                false, null,
                AppMessage.UnexpectedError,
                AppMessage.UnexpectedError.GetMessage());
        }
    }
}