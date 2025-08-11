using MediatR;
using NotificationService.ApiContract.Request;
using NotificationService.ApiContract.Response;
using Shared.CrossCutting.Interfaces;

namespace NotificationService.ApplicationService.Handlers.Commands
{
    public class SendNotificationCommand : ICommand<SendNotificationResponse>
    {
        public SendNotificationRequest Request { get; }

        public SendNotificationCommand(SendNotificationRequest request)
        {
            Request = request;
        }
    }
}
