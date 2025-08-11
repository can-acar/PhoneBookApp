using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService.Tests.Handlers.Queries
{
    public class GetUserNotificationsQueryHandlerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILogger<GetUserNotificationsQueryHandler>> _mockLogger;
        private readonly GetUserNotificationsQueryHandler _handler;

        public GetUserNotificationsQueryHandlerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<GetUserNotificationsQueryHandler>>();
            _handler = new GetUserNotificationsQueryHandler(_mockNotificationService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserId_ShouldReturnUserNotifications()
        {
            // Arrange
            var userId = "user123";
            var query = new GetUserNotificationsQuery(userId);

            var notifications = new List<Notification>
            {
                CreateTestNotification(userId, "Subject 1", "corr-1"),
                CreateTestNotification(userId, "Subject 2", "corr-2"),
                CreateTestNotification(userId, "Subject 3", "corr-3")
            };

            _mockNotificationService
                .Setup(x => x.GetUserNotificationsAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Notifications.Should().HaveCount(3);
            
            var responseNotifications = result.Data.Notifications.ToList();
            responseNotifications.All(n => n.UserId == userId).Should().BeTrue();
            responseNotifications[0].Subject.Should().Be("Subject 1");
            responseNotifications[1].Subject.Should().Be("Subject 2");
            responseNotifications[2].Subject.Should().Be("Subject 3");

            _mockNotificationService.Verify(
                x => x.GetUserNotificationsAsync(userId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithUserHavingNoNotifications_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = "user123";
            var query = new GetUserNotificationsQuery(userId);

            _mockNotificationService
                .Setup(x => x.GetUserNotificationsAsync(userId, It.IsAny<CancellationToken>()))
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
            var userId = "user123";
            var query = new GetUserNotificationsQuery(userId);

            _mockNotificationService
                .Setup(x => x.GetUserNotificationsAsync(userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Database connection failed");
        }

        [Fact]
        public async Task Handle_WithMixedNotificationStates_ShouldReturnCorrectStatuses()
        {
            // Arrange
            var userId = "user123";
            var query = new GetUserNotificationsQuery(userId);

            var sentNotification = CreateTestNotification(userId, "Sent Subject", "corr-1");
            sentNotification.SetRecipientEmail("test@example.com");
            sentNotification.MarkAsSent(ProviderType.Email);

            var failedNotification = CreateTestNotification(userId, "Failed Subject", "corr-2");
            failedNotification.MarkAsFailed("Network error");

            var pendingNotification = CreateTestNotification(userId, "Pending Subject", "corr-3");

            var notifications = new List<Notification> { sentNotification, failedNotification, pendingNotification };

            _mockNotificationService
                .Setup(x => x.GetUserNotificationsAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            var responseNotifications = result.Data!.Notifications.ToList();
            responseNotifications.Should().HaveCount(3);

            // Check sent notification
            var sentResponse = responseNotifications.FirstOrDefault(n => n.Subject == "Sent Subject");
            sentResponse.Should().NotBeNull();
            sentResponse!.Status.Should().Be("Delivered");
            sentResponse.SentAt.Should().NotBeNull();

            // Check failed notification
            var failedResponse = responseNotifications.FirstOrDefault(n => n.Subject == "Failed Subject");
            failedResponse.Should().NotBeNull();
            failedResponse!.Status.Should().Be("Failed");
            failedResponse.ErrorMessage.Should().Be("Network error");

            // Check pending notification
            var pendingResponse = responseNotifications.FirstOrDefault(n => n.Subject == "Pending Subject");
            pendingResponse.Should().NotBeNull();
            pendingResponse!.Status.Should().Be("Failed"); // Not delivered means failed in the mapping
        }

        [Fact]
        public async Task Handle_WithNotificationsHavingDifferentPriorities_ShouldIncludePriorities()
        {
            // Arrange
            var userId = "user123";
            var query = new GetUserNotificationsQuery(userId);

            var notifications = new List<Notification>
            {
                new Notification(userId, "Low Priority", "Content", NotificationPriority.Low, "corr-1"),
                new Notification(userId, "High Priority", "Content", NotificationPriority.High, "corr-2"),
                new Notification(userId, "Critical Priority", "Content", NotificationPriority.Critical, "corr-3")
            };

            _mockNotificationService
                .Setup(x => x.GetUserNotificationsAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            var responseNotifications = result.Data!.Notifications.ToList();
            responseNotifications.Should().HaveCount(3);

            responseNotifications.FirstOrDefault(n => n.Subject == "Low Priority")?.Priority.Should().Be(NotificationPriority.Low);
            responseNotifications.FirstOrDefault(n => n.Subject == "High Priority")?.Priority.Should().Be(NotificationPriority.High);
            responseNotifications.FirstOrDefault(n => n.Subject == "Critical Priority")?.Priority.Should().Be(NotificationPriority.Critical);
        }

        [Fact]
        public async Task Handle_WithNotificationsHavingDifferentProviders_ShouldIncludeProviders()
        {
            // Arrange
            var userId = "user123";
            var query = new GetUserNotificationsQuery(userId);

            var emailNotification = CreateTestNotification(userId, "Email Subject", "corr-1");
            emailNotification.SetRecipientEmail("test@example.com");

            var smsNotification = CreateTestNotification(userId, "SMS Subject", "corr-2");
            smsNotification.SetRecipientPhoneNumber("+1234567890");

            var notifications = new List<Notification> { emailNotification, smsNotification };

            _mockNotificationService
                .Setup(x => x.GetUserNotificationsAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            var responseNotifications = result.Data!.Notifications.ToList();
            responseNotifications.Should().HaveCount(2);

            var emailResponse = responseNotifications.FirstOrDefault(n => n.Subject == "Email Subject");
            emailResponse.Should().NotBeNull();
            emailResponse!.PreferredProvider.Should().Be(ProviderType.Email);
            emailResponse.RecipientEmail.Should().Be("test@example.com");

            var smsResponse = responseNotifications.FirstOrDefault(n => n.Subject == "SMS Subject");
            smsResponse.Should().NotBeNull();
            smsResponse!.PreferredProvider.Should().Be(ProviderType.Sms);
            smsResponse.RecipientPhoneNumber.Should().Be("+1234567890");
        }

        private static Notification CreateTestNotification(string userId, string subject, string correlationId)
        {
            return new Notification(userId, subject, "Test Content", NotificationPriority.Normal, correlationId);
        }
    }
}
