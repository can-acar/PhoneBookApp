using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Contracts;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Enums;
using ContactService.Domain;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Extensions;
using Shared.CrossCutting.Models;
using Shared.CrossCutting.Interfaces;

namespace ContactService.ApplicationService.Handlers.Commands
{
    public class CreateContactHandler : ICommandHandler<CreateContactCommand, Guid>
    {
        private readonly IContactService _contactService;

        public CreateContactHandler(IContactService contactService)
        {
            _contactService = contactService;
        }

        public async Task<ApiResponse<Guid>> Handle(CreateContactCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Contact info list oluştur
                var contactInfos = request.ContactInfos.Select(ci => new ContactInfo(
                    Guid.Empty, // ContactId will be set by the service
                    (ContactInfoType)ci.InfoType,
                    ci.InfoValue
                ));

                var createdContact = await _contactService.CreateContactAsync(request.FirstName, request.LastName, request.Company, contactInfos, cancellationToken);

                if (createdContact == null)
                {
                    return ApiResponse.Result<Guid>(
                        false,
                        Guid.Empty,
                        AppMessage.CreateContactFailed,
                        AppMessage.CreateContactFailed.GetMessage());
                }


                return ApiResponse.Result(
                    true,
                    createdContact.Id,
                    AppMessage.ContactCreatedSuccessfully,
                    AppMessage.ContactCreatedSuccessfully.GetMessage());
            }
            catch (Exception ex)
            {
                return ApiResponse.Result(
                    false,
                    Guid.Empty,
                    AppMessage.UnexpectedError, AppMessage.UnexpectedError.GetMessage());
            }
        }
    }
}