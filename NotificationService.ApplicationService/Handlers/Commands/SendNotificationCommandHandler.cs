using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.ApiContract.Response;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace NotificationService.ApplicationService.Handlers.Commands;

public class SendNotificationCommandHandler : ICommandHandler<SendNotificationCommand, SendNotificationResponse>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        INotificationService notificationService,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ApiResponse<SendNotificationResponse>> Handle(SendNotificationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var request = command.Request;
            
            // Use rich domain constructor
            var notification = new Notification(
                userId: request.UserId,
                subject: request.Subject,
                content: request.Content,
                priority: request.Priority,
                correlationId: request.CorrelationId ?? Guid.NewGuid().ToString()
            );

            // Set recipient information using domain methods
            if (!string.IsNullOrWhiteSpace(request.RecipientEmail))
            {
                notification.SetRecipientEmail(request.RecipientEmail);
            }

            if (!string.IsNullOrWhiteSpace(request.RecipientPhoneNumber))
            {
                notification.SetRecipientPhoneNumber(request.RecipientPhoneNumber);
            }

            // Set preferred provider if specified
            if (request.PreferredProvider != Domain.Enums.ProviderType.Unknown)
            {
                notification.SetPreferredProvider(request.PreferredProvider);
            }

            // Add additional data using domain method
            if (request.AdditionalData != null)
            {
                foreach (var kvp in request.AdditionalData)
                {
                    notification.AddAdditionalData(kvp.Key, kvp.Value);
                }
            }

            var notificationId = await _notificationService.CreateNotificationAsync(notification, cancellationToken);
            var result = await _notificationService.SendNotificationAsync(notification, cancellationToken);

            var response = new SendNotificationResponse 
            { 
                Id = notificationId.ToString(), 
                Success = result.Success, 
                ErrorMessage = result.ErrorMessage,
                SentAt = result.SentAt
            };

            return ApiResponse.Result<SendNotificationResponse>(
                true,
                response,
                200,
                "Notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            
            return ApiResponse.Result<SendNotificationResponse>(
                false,
                null,
                500,
                "An error occurred while sending the notification");
        }
    }
}