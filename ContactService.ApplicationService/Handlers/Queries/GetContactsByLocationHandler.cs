using ContactService.ApiContract.Contracts;
using ContactService.ApiContract.Request.Queries;
using ContactService.ApiContract.Response.Queries;
using ContactService.Domain;
using ContactService.Domain.Interfaces;
using MediatR;
using Shared.CrossCutting.Extensions;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApplicationService.Handlers.Queries;

public class GetContactsByLocationHandler : IQueryHandler<GetContactsByLocationQuery, PageResponse<ContactDto>>
{
    private readonly IContactService _contactService;

    public GetContactsByLocationHandler(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<ApiResponse<PageResponse<ContactDto>>> Handle(GetContactsByLocationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var contacts = await _contactService.GetContactsFilterByLocation(
                request.Page,
                request.PageSize,
                request.Location,
                cancellationToken);

            var result = contacts.Data.Select(contact => new ContactDto
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Company = contact.Company,
                ContactInfos = contact.ContactInfosReadOnly.Select(ci => new ContactInfoDto
                {
                    Id = ci.Id,
                    InfoType = ci.InfoType.GetKey(),
                    InfoValue = ci.Content
                }).ToList()
            }).ToList();

            var pageResponse = PageResponse.Result(result, contacts.PageNumber, contacts.PageSize, contacts.TotalCount, AppMessage.ContactsListedSuccessfully,
                AppMessage.ContactsListedSuccessfully.GetMessage());

            return pageResponse;
        }
        catch (Exception)
        {
            return ApiResponse.Result<PageResponse<ContactDto>>(
                false, null,
                AppMessage.UnexpectedError,
                AppMessage.UnexpectedError.GetMessage());
        }
    }
}