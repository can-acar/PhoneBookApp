using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NotificationService.Domain.Entities;

namespace NotificationService.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _mockRepository;
        private readonly Mock<INotificationProviderManager> _mockProviderManager;
        private readonly Mock<ILogger<ApplicationService.Services.NotificationService>> _mockLogger;
        private readonly ApplicationService.Services.NotificationService _service;

        public NotificationServiceTests()
        {
            _mockRepository = new Mock<INotificationRepository>();
            _mockProviderManager = new Mock<INotificationProviderManager>();
            _mockLogger = new Mock<ILogger<ApplicationService.Services.NotificationService>>();
            _service = new ApplicationService.Services.NotificationService(
                _mockRepository.Object, 
                _mockProviderManager.Object, 
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateNotificationAsync_WithValidNotification_ShouldCreateAndReturnId()
        {
            // Arrange
            var notification = CreateTestNotification();

            _mockRepository
                .Setup(x => x.CreateAsync(notification))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().Be(notification.Id);
            _mockRepository.Verify(x => x.CreateAsync(notification), Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WithSuccessfulSend_ShouldMarkAsSentAndUpdate()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            var successResult = new NotificationResult
            {
                Success = true,
                SentAt = DateTime.UtcNow
            };

            _mockProviderManager
                .Setup(x => x.SendNotificationAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(successResult);

            _mockRepository
                .Setup(x => x.UpdateAsync(notification))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            notification.IsDelivered.Should().BeTrue();
            notification.HasBeenSent.Should().BeTrue();

            _mockProviderManager.Verify(
                x => x.SendNotificationAsync(notification, It.IsAny<CancellationToken>()),
                Times.Once);
            
            _mockRepository.Verify(x => x.UpdateAsync(notification), Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WithFailedSend_ShouldMarkAsFailedAndUpdate()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            var failureResult = new NotificationResult
            {
                Success = false,
                ErrorMessage = "Send failed"
            };

            _mockProviderManager
                .Setup(x => x.SendNotificationAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(failureResult);

            _mockRepository
                .Setup(x => x.UpdateAsync(notification))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Send failed");
            notification.IsDelivered.Should().BeFalse();
            notification.HasFailed.Should().BeTrue();
            notification.ErrorMessage.Should().Be("Send failed");

            _mockProviderManager.Verify(
                x => x.SendNotificationAsync(notification, It.IsAny<CancellationToken>()),
                Times.Once);
            
            _mockRepository.Verify(x => x.UpdateAsync(notification), Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WhenExceptionThrown_ShouldMarkAsFailedAndReturnError()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            var exception = new InvalidOperationException("Provider unavailable");

            _mockProviderManager
                .Setup(x => x.SendNotificationAsync(notification, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            _mockRepository
                .Setup(x => x.UpdateAsync(notification))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Provider unavailable");
            notification.HasFailed.Should().BeTrue();
            notification.ErrorMessage.Should().Be("Exception: Provider unavailable");

            _mockRepository.Verify(x => x.UpdateAsync(notification), Times.Once);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_WithValidUserId_ShouldReturnNotifications()
        {
            // Arrange
            var userId = "user123";
            var notifications = new List<Notification>
            {
                CreateTestNotification(userId, "Subject 1"),
                CreateTestNotification(userId, "Subject 2")
            };

            _mockRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(notifications);

            // Act
            var result = await _service.GetUserNotificationsAsync(userId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(n => n.UserId == userId).Should().BeTrue();

            _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_WithUserHavingNoNotifications_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = "user123";

            _mockRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Notification>());

            // Act
            var result = await _service.GetUserNotificationsAsync(userId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByCorrelationIdAsync_WithValidCorrelationId_ShouldReturnNotifications()
        {
            // Arrange
            var correlationId = "corr-123";
            var notifications = new List<Notification>
            {
                CreateTestNotification("user1", "Subject 1", correlationId),
                CreateTestNotification("user2", "Subject 2", correlationId)
            };

            _mockRepository
                .Setup(x => x.GetByCorrelationIdAsync(correlationId))
                .ReturnsAsync(notifications);

            // Act
            var result = await _service.GetByCorrelationIdAsync(correlationId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(n => n.CorrelationId == correlationId).Should().BeTrue();

            _mockRepository.Verify(x => x.GetByCorrelationIdAsync(correlationId), Times.Once);
        }

        [Fact]
        public async Task GetByCorrelationIdAsync_WithNoMatchingNotifications_ShouldReturnEmptyList()
        {
            // Arrange
            var correlationId = "corr-123";

            _mockRepository
                .Setup(x => x.GetByCorrelationIdAsync(correlationId))
                .ReturnsAsync(new List<Notification>());

            // Act
            var result = await _service.GetByCorrelationIdAsync(correlationId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockRepository.Verify(x => x.GetByCorrelationIdAsync(correlationId), Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_ShouldLogAppropriateMessages()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            var successResult = new NotificationResult { Success = true };

            _mockProviderManager
                .Setup(x => x.SendNotificationAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(successResult);

            _mockRepository
                .Setup(x => x.UpdateAsync(notification))
                .Returns(Task.CompletedTask);

            // Act
            await _service.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending notification")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            var exception = new InvalidOperationException("Test exception");

            _mockProviderManager
                .Setup(x => x.SendNotificationAsync(notification, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            _mockRepository
                .Setup(x => x.UpdateAsync(notification))
                .Returns(Task.CompletedTask);

            // Act
            await _service.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send notification")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_ShouldLogInformationMessage()
        {
            // Arrange
            var userId = "user123";

            _mockRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Notification>());

            // Act
            await _service.GetUserNotificationsAsync(userId, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting notifications for user")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetByCorrelationIdAsync_ShouldLogInformationMessage()
        {
            // Arrange
            var correlationId = "corr-123";

            _mockRepository
                .Setup(x => x.GetByCorrelationIdAsync(correlationId))
                .ReturnsAsync(new List<Notification>());

            // Act
            await _service.GetByCorrelationIdAsync(correlationId, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting notifications by correlation ID")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static Notification CreateTestNotification(string userId = "user123", string subject = "Test Subject", string correlationId = "corr-123")
        {
            return new Notification(userId, subject, "Test Content", NotificationPriority.Normal, correlationId);
        }
    }
}
