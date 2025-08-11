using ContactService.ApiContract.Contracts;
using ContactService.ApiContract.Request.Queries;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Entities;
using ContactService.Domain;
using ContactService.Domain.Enums;
using Shared.CrossCutting.Models;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Extensions;

namespace ContactService.ApplicationService.Handlers.Queries;

public class GetAllContactsHandler : IQueryHandler<GetAllContactsQuery, PageResponse<ContactSummaryDto>>
{
    private readonly IContactService _contactService;

    public GetAllContactsHandler(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<ApiResponse<PageResponse<ContactSummaryDto>>> Handle(GetAllContactsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _contactService.GetAllContactsAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                cancellationToken);

            if (result == null || !result.Data.Any())
            {
                // Manuel conversion - implicit operator çalışmıyor
                return ApiResponse.Result<PageResponse<ContactSummaryDto>>(
                    false,null,
                    AppMessage.NotFoundData,
                    AppMessage.NotFoundData.GetMessage()
                );
            }

            // Contact'ları ContactSummaryDto'ya mapping yap
            var contactSummaries = result.Data.Select(contact => new ContactSummaryDto
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Company = contact.Company,
                ContactInfoCount = contact.ContactInfos.Count(),
                Contacts = contact.ContactInfos.Select(ci => new ContactInfoDto
                {
                    Id = ci.Id,
                    InfoType = EnumHelperExtension.GetKey(ci.InfoType),
                    InfoValue = ci.Content
                }).ToList()
            }).ToList();

            // PageResponse oluştur
            return PageResponse.Result(
                contactSummaries,
                request.Page,
                result.PageSize,
                result.TotalCount,
                AppMessage.ContactsListedSuccessfully,
                AppMessage.ContactsListedSuccessfully.GetMessage());

            // Manuel conversion
        }
        catch (Exception ex)
        {
            var errorResponse = PageResponse<ContactSummaryDto>.CreateError(
                request.Page,
                request.PageSize,
                ex.Message);

            // Manuel conversion
            return ApiResponse.Result(
                errorResponse.Success,
                errorResponse,
                errorResponse.Success ? 200 : 500,
                errorResponse.Message ?? "Hata oluştu"
            );
        }
    }
}