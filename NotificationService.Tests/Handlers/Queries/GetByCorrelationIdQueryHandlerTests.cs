using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService.Tests.Handlers.Queries
{
    public class GetByCorrelationIdQueryHandlerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILogger<GetByCorrelationIdQueryHandler>> _mockLogger;
        private readonly GetByCorrelationIdQueryHandler _handler;

        public GetByCorrelationIdQueryHandlerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<GetByCorrelationIdQueryHandler>>();
            _handler = new GetByCorrelationIdQueryHandler(_mockNotificationService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidCorrelationId_ShouldReturnNotifications()
        {
            // Arrange
            var correlationId = "corr-123";
            var query = new GetByCorrelationIdQuery(correlationId);

            var notifications = new List<Notification>
            {
                CreateTestNotification("user1", "Subject 1", correlationId),
                CreateTestNotification("user2", "Subject 2", correlationId)
            };

            _mockNotificationService
                .Setup(x => x.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Notifications.Should().HaveCount(2);
            
            var responseNotifications = result.Data.Notifications.ToList();
            responseNotifications[0].UserId.Should().Be("user1");
            responseNotifications[0].Subject.Should().Be("Subject 1");
            responseNotifications[1].UserId.Should().Be("user2");
            responseNotifications[1].Subject.Should().Be("Subject 2");

            _mockNotificationService.Verify(
                x => x.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithNoNotifications_ShouldReturnEmptyList()
        {
            // Arrange
            var correlationId = "corr-123";
            var query = new GetByCorrelationIdQuery(correlationId);

            _mockNotificationService
                .Setup(x => x.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Notification>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Notifications.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WhenServiceThrowsException_ShouldReturnErrorResponse()
        {
            // Arrange
            var correlationId = "corr-123";
            var query = new GetByCorrelationIdQuery(correlationId);

            _mockNotificationService
                .Setup(x => x.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Database error");
        }

        [Fact]
        public async Task Handle_WithDeliveredNotification_ShouldSetCorrectStatus()
        {
            // Arrange
            var correlationId = "corr-123";
            var query = new GetByCorrelationIdQuery(correlationId);

            var notification = CreateTestNotification("user1", "Subject 1", correlationId);
            notification.SetRecipientEmail("test@example.com");
            notification.MarkAsSent(ProviderType.Email);

            var notifications = new List<Notification> { notification };

            _mockNotificationService
                .Setup(x => x.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            var responseNotification = result.Data!.Notifications.First();
            responseNotification.Status.Should().Be("Delivered");
            responseNotification.SentAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithFailedNotification_ShouldSetCorrectStatus()
        {
            // Arrange
            var correlationId = "corr-123";
            var query = new GetByCorrelationIdQuery(correlationId);

            var notification = CreateTestNotification("user1", "Subject 1", correlationId);
            notification.MarkAsFailed("Send failed");

            var notifications = new List<Notification> { notification };

            _mockNotificationService
                .Setup(x => x.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            var responseNotification = result.Data!.Notifications.First();
            responseNotification.Status.Should().Be("Failed");
            responseNotification.ErrorMessage.Should().Be("Send failed");
        }

        [Fact]
        public async Task Handle_WithNotificationWithAdditionalData_ShouldIncludeAdditionalData()
        {
            // Arrange
            var correlationId = "corr-123";
            var query = new GetByCorrelationIdQuery(correlationId);

            var notification = CreateTestNotification("user1", "Subject 1", correlationId);
            notification.AddAdditionalData("key1", "value1");
            notification.AddAdditionalData("key2", "value2");

            var notifications = new List<Notification> { notification };

            _mockNotificationService
                .Setup(x => x.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            var responseNotification = result.Data!.Notifications.First();
            responseNotification.AdditionalData.Should().NotBeNull();
            responseNotification.AdditionalData!.Should().ContainKey("key1");
            responseNotification.AdditionalData["key1"].Should().Be("value1");
            responseNotification.AdditionalData.Should().ContainKey("key2");
            responseNotification.AdditionalData["key2"].Should().Be("value2");
        }

        private static Notification CreateTestNotification(string userId, string subject, string correlationId)
        {
            return new Notification(userId, subject, "Test Content", NotificationPriority.Normal, correlationId);
        }
    }
}
