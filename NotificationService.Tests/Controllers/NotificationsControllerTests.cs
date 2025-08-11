using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Api.Controllers;
using NotificationService.ApiContract.Request;
using NotificationService.ApiContract.Response;
using NotificationService.ApplicationService.Handlers.Commands;
using NotificationService.ApplicationService.Handlers.Queries;
using NotificationService.Domain.Enums;

namespace NotificationService.Tests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<NotificationsController>> _loggerMock;
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<NotificationsController>>();
        _controller = new NotificationsController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserNotifications_ShouldReturnOkResult_WhenUserHasNotifications()
    {
                // Arrange
        var userId = "user123";
        var expectedResponse = new GetUserNotificationsResponse
        {
            Notifications = new List<NotificationResponse>
            {
                new NotificationResponse
                {
                    Id = "notif1",
                    UserId = userId,
                    Subject = "Test Subject",
                    Content = "Test Content",
                    Status = "Sent",
                    Priority = NotificationPriority.Normal,
                    PreferredProvider = ProviderType.Email,
                    CreatedAt = DateTime.UtcNow,
                    CorrelationId = "corr123"
                }
            }
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetUserNotifications(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<GetUserNotificationsResponse>;
        response.Should().NotBeNull();
        response.Data.Notifications.Should().HaveCount(1);
        response.Data.Notifications[0].Id.Should().Be("notif1");

        _mediatorMock.Verify(m => m.Send(
            It.Is<GetUserNotificationsQuery>(q => q.UserId == userId), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetUserNotifications_ShouldReturnOkResult_WhenUserHasNoNotifications()
    {
        // Arrange
        var userId = "user456";
        var expectedResponse = new GetUserNotificationsResponse
        {
            Notifications = new List<NotificationResponse>()
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetUserNotifications(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value as GetUserNotificationsResponse;
        response.Should().NotBeNull();
        response!.Notifications.Should().BeEmpty();

        _mediatorMock.Verify(m => m.Send(
            It.Is<GetUserNotificationsQuery>(q => q.UserId == userId), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetUserNotifications_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = "user789";
        var exception = new Exception("Database connection failed");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetUserNotifications(userId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while retrieving notifications" });

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving notifications for user")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByCorrelationId_ShouldReturnOkResult_WhenNotificationsExist()
    {
        // Arrange
        var correlationId = "corr-123-456";
        var expectedResponse = new GetByCorrelationIdResponse
        {
            Notifications = new List<NotificationResponse>
            {
                new NotificationResponse
                {
                    Id = "notif1",
                    UserId = "user1",
                    Subject = "Report Generated",
                    Content = "Your report is ready",
                    Status = "Sent",
                    Priority = NotificationPriority.High,
                    PreferredProvider = ProviderType.Email,
                    CreatedAt = DateTime.UtcNow,
                    CorrelationId = correlationId
                }
            }
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetByCorrelationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetByCorrelationId(correlationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(m => m.Send(
            It.Is<GetByCorrelationIdQuery>(q => q.CorrelationId == correlationId), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetByCorrelationId_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var correlationId = "corr-789";
        var exception = new InvalidOperationException("Service unavailable");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetByCorrelationIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetByCorrelationId(correlationId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while retrieving notifications" });

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving notifications for correlation ID")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotification_ShouldReturnOkResult_WhenNotificationSentSuccessfully()
    {
        // Arrange
        var request = new SendNotificationRequest
        {
            UserId = "user123",
            Subject = "Test Notification",
            Content = "This is a test notification",
            RecipientEmail = "user@example.com",
            Priority = NotificationPriority.Normal,
            PreferredProvider = ProviderType.Email,
            CorrelationId = "corr-123"
        };

        var expectedResponse = new SendNotificationResponse
        {
            Id = "notif-456",
            Success = true,
            SentAt = DateTime.UtcNow
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendNotification(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(m => m.Send(
            It.Is<SendNotificationCommand>(cmd => cmd.Request == request), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task SendNotification_ShouldReturnOkResult_WhenNotificationFails()
    {
        // Arrange
        var request = new SendNotificationRequest
        {
            UserId = "user456",
            Subject = "Test Notification",
            Content = "This notification will fail",
            RecipientEmail = "invalid-email",
            Priority = NotificationPriority.High,
            PreferredProvider = ProviderType.Email
        };

        var expectedResponse = new SendNotificationResponse
        {
            Id = "notif-789",
            Success = false,
            ErrorMessage = "Invalid email address"
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendNotification(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task SendNotification_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var request = new SendNotificationRequest
        {
            UserId = "user789",
            Subject = "Test",
            Content = "Test content"
        };

        var exception = new ArgumentException("Invalid request");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.SendNotification(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while sending notification" });

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error sending notification")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(NotificationPriority.Low, ProviderType.Email)]
    [InlineData(NotificationPriority.Normal, ProviderType.Sms)]
    [InlineData(NotificationPriority.High, ProviderType.WebSocket)]
    [InlineData(NotificationPriority.Critical, ProviderType.Push)]
    public async Task SendNotification_ShouldAccept_DifferentPriorityAndProviderCombinations(
        NotificationPriority priority, ProviderType provider)
    {
        // Arrange
        var request = new SendNotificationRequest
        {
            UserId = "user123",
            Subject = "Test",
            Content = "Test content",
            Priority = priority,
            PreferredProvider = provider
        };

        var response = new SendNotificationResponse
        {
            Id = "notif-123",
            Success = true
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.SendNotification(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mediatorMock.Verify(m => m.Send(
            It.Is<SendNotificationCommand>(cmd => 
                cmd.Request.Priority == priority && 
                cmd.Request.PreferredProvider == provider), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task SendNotification_ShouldPass_AdditionalDataCorrectly()
    {
        // Arrange
        var additionalData = new Dictionary<string, string>
        {
            { "reportId", "report-123" },
            { "template", "report-complete" }
        };

        var request = new SendNotificationRequest
        {
            UserId = "user123",
            Subject = "Report Ready",
            Content = "Your report is ready for download",
            AdditionalData = additionalData,
            CorrelationId = "corr-456"
        };

        var response = new SendNotificationResponse { Id = "notif-789", Success = true };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.SendNotification(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mediatorMock.Verify(m => m.Send(
            It.Is<SendNotificationCommand>(cmd => 
                cmd.Request.AdditionalData != null &&
                cmd.Request.AdditionalData["reportId"] == "report-123" &&
                cmd.Request.AdditionalData["template"] == "report-complete" &&
                cmd.Request.CorrelationId == "corr-456"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("user-with-long-id-12345")]
    [InlineData("123")]
    public async Task GetUserNotifications_ShouldAccept_DifferentUserIdFormats(string userId)
    {
        // Arrange
        var response = new GetUserNotificationsResponse
        {
            Notifications = new List<NotificationResponse>()
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetUserNotifications(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetUserNotificationsQuery>(q => q.UserId == userId), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
