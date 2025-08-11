using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationService.ApiContract.Request;
using NotificationService.ApplicationService.Handlers.Commands;
using NotificationService.ApplicationService.Handlers.Queries;

namespace NotificationService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            IMediator mediator,
            ILogger<NotificationsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(string userId)
        {
            try
            {
                var query = new GetUserNotificationsQuery(userId);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
            }
        }

        [HttpGet("correlation/{correlationId}")]
        public async Task<IActionResult> GetByCorrelationId(string correlationId)
        {
            try
            {
                var query = new GetByCorrelationIdQuery(correlationId);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for correlation ID {CorrelationId}", correlationId);
                return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            try
            {
                var command = new SendNotificationCommand(request);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return StatusCode(500, new { message = "An error occurred while sending notification" });
            }
        }
    }
}
