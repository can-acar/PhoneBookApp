using MediatR;
using NotificationService.ApiContract.Request;
using NotificationService.ApiContract.Response;
using Shared.CrossCutting.Interfaces;

namespace NotificationService.ApplicationService.Handlers.Queries
{
    public class GetUserNotificationsQuery : IQuery<GetUserNotificationsResponse>
    {
        public string UserId { get; }

        public GetUserNotificationsQuery(string userId)
        {
            UserId = userId;
        }
    }
}
