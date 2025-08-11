using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.ApiContract.Response;
using NotificationService.Domain.Interfaces;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace NotificationService.ApplicationService.Handlers.Queries;

public class GetByCorrelationIdQueryHandler : IQueryHandler<GetByCorrelationIdQuery, GetByCorrelationIdResponse>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<GetByCorrelationIdQueryHandler> _logger;

    public GetByCorrelationIdQueryHandler(
        INotificationService notificationService,
        ILogger<GetByCorrelationIdQueryHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ApiResponse<GetByCorrelationIdResponse>> Handle(GetByCorrelationIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await _notificationService.GetByCorrelationIdAsync(query.CorrelationId, cancellationToken);
                
            var response = new GetByCorrelationIdResponse
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

            return ApiResponse.Result<GetByCorrelationIdResponse>(
                true,
                response,
                200,
                "Notifications retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for correlation ID {CorrelationId}", query.CorrelationId);
            
            return ApiResponse.Result<GetByCorrelationIdResponse>(
                false,
                null,
                500,
                "An error occurred while retrieving notifications");
        }
    }
}