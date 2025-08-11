using ContactService.ApiContract.Request.Queries;
using ContactService.ApiContract.Response.Queries;
using ContactService.Domain;
using ContactService.Domain.Interfaces;
using MediatR;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApplicationService.Handlers.Queries
{
    public class GetLocationStatisticsQueryHandler : IQueryHandler<GetLocationStatisticsQuery, List<LocationStatisticsResponse>>
    {
        private readonly IContactService _contactService;

        public GetLocationStatisticsQueryHandler(IContactService contactService)
        {
            _contactService = contactService;
        }

        public async Task<ApiResponse<List<LocationStatisticsResponse>>> Handle(GetLocationStatisticsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var statistics = await _contactService.GetLocationStatistics(cancellationToken);
                
                var response = statistics.Select(s => new LocationStatisticsResponse
                {
                    Location = s.Location,
                    ContactCount = s.ContactCount,
                    PhoneNumberCount = s.PhoneNumberCount
                }).ToList();

                return ApiResponse.Result<List<LocationStatisticsResponse>>(
                    true, response,
                    AppMessage.LocationStatisticsRetrievedSuccessfully,
                    AppMessage.LocationStatisticsRetrievedSuccessfully.GetMessage());
            }
            catch (Exception ex)
            {
                return ApiResponse.Result<List<LocationStatisticsResponse>> (
                    false, null,
                    AppMessage.UnexpectedError,
                    AppMessage.UnexpectedError.GetMessage());
            }
        }
    }
}
