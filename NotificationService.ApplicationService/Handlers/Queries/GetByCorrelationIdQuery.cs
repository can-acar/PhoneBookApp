using MediatR;
using NotificationService.ApiContract.Request;
using NotificationService.ApiContract.Response;
using Shared.CrossCutting.Interfaces;

namespace NotificationService.ApplicationService.Handlers.Queries
{
    public class GetByCorrelationIdQuery : IQuery<GetByCorrelationIdResponse>
    {
        public string CorrelationId { get; }

        public GetByCorrelationIdQuery(string correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
