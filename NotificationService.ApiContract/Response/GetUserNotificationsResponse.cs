using System.Collections.Generic;
using NotificationService.ApiContract.Response;

namespace NotificationService.ApiContract.Response
{
    public class GetUserNotificationsResponse
    {
        public List<NotificationResponse> Notifications { get; set; } = new();
    }
}
