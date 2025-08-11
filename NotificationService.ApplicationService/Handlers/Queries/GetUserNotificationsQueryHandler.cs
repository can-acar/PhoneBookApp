using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.ApiContract.Response;
using NotificationService.Domain.Interfaces;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace NotificationService.ApplicationService.Handlers.Queries;

public class GetUserNotificationsQueryHandler : IQueryHandler<GetUserNotificationsQuery, GetUserNotificationsResponse>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<GetUserNotificationsQueryHandler> _logger;

    public GetUserNotificationsQueryHandler(
        INotificationService notificationService,
        ILogger<GetUserNotificationsQueryHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ApiResponse<GetUserNotificationsResponse>> Handle(GetUserNotificationsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(query.UserId, cancellationToken);
                
            var response = new GetUserNotificationsResponse
            {
                Notifications = notifications.Select(n => new NotificationResponse
                {
                    Id = n.Id.ToString(),
                    UserId = n.UserId,
                    Subject = n.Subject,
                    Content = n.Content,
                    RecipientEmail = n.RecipientEmail,
                    RecipientPhoneNumber = n.RecipientPhoneNumber,
                    Priority = n.Priority,
                    PreferredProvider = n.PreferredProvider,
                    AdditionalData = n.AdditionalData,
                    Status = n.IsDelivered ? "Delivered" : "Failed",
                    ErrorMessage = n.ErrorMessage,
                    CreatedAt = n.CreatedAt,
                    SentAt = n.SentAt,
                    CorrelationId = n.CorrelationId
                }).ToList()
            };

            return ApiResponse.Result<GetUserNotificationsResponse>(
                true,
                response,
                200,
                "User notifications retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", query.UserId);
            
            return ApiResponse.Result<GetUserNotificationsResponse>(
                false,
                null,
                500,
                "An error occurred while retrieving user notifications");
        }
    }
}