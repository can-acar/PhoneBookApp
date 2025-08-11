using NotificationService.Domain.Enums;

namespace NotificationService.ApiContract.Request
{
    public class GetUserNotificationsRequest
    {
        public string UserId { get; set; } = string.Empty;
    }
}
