using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NotificationService.ApplicationService.Services;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Tests.Services
{
    public class NotificationProviderManagerTests
    {
        private readonly Mock<INotificationProvider> _mockEmailProvider;
        private readonly Mock<INotificationProvider> _mockSmsProvider;
        private readonly Mock<ILogger<NotificationProviderManager>> _mockLogger;
        private readonly NotificationProviderManager _manager;

        public NotificationProviderManagerTests()
        {
            _mockEmailProvider = new Mock<INotificationProvider>();
            _mockSmsProvider = new Mock<INotificationProvider>();
            _mockLogger = new Mock<ILogger<NotificationProviderManager>>();

            _mockEmailProvider.Setup(x => x.ProviderType).Returns(ProviderType.Email);
            _mockEmailProvider.Setup(x => x.Priority).Returns(NotificationPriority.High);
            _mockEmailProvider.Setup(x => x.IsEnabled).Returns(true);
            _mockEmailProvider.Setup(x => x.IsHealthy).Returns(true);

            _mockSmsProvider.Setup(x => x.ProviderType).Returns(ProviderType.Sms);
            _mockSmsProvider.Setup(x => x.Priority).Returns(NotificationPriority.Normal);
            _mockSmsProvider.Setup(x => x.IsEnabled).Returns(true);
            _mockSmsProvider.Setup(x => x.IsHealthy).Returns(true);

            var providers = new List<INotificationProvider> { _mockEmailProvider.Object, _mockSmsProvider.Object };
            _manager = new NotificationProviderManager(providers, _mockLogger.Object);
        }

        [Fact]
        public void GetAllProviders_ShouldReturnAllProviders()
        {
            // Act
            var result = _manager.GetAllProviders();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.ProviderType == ProviderType.Email);
            result.Should().Contain(p => p.ProviderType == ProviderType.Sms);
        }

        [Fact]
        public void GetActiveProviders_WithAllProvidersEnabled_ShouldReturnAllProviders()
        {
            // Act
            var result = _manager.GetActiveProviders();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.ProviderType == ProviderType.Email);
            result.Should().Contain(p => p.ProviderType == ProviderType.Sms);
        }

        [Fact]
        public void GetActiveProviders_WithSomeProvidersDisabled_ShouldReturnOnlyEnabledProviders()
        {
            // Arrange
            _mockSmsProvider.Setup(x => x.IsEnabled).Returns(false);

            // Act
            var result = _manager.GetActiveProviders();

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(p => p.ProviderType == ProviderType.Email);
            result.Should().NotContain(p => p.ProviderType == ProviderType.Sms);
        }

        [Fact]
        public void GetActiveProviders_WithSomeProvidersUnhealthy_ShouldReturnOnlyHealthyProviders()
        {
            // Arrange
            _mockSmsProvider.Setup(x => x.IsHealthy).Returns(false);

            // Act
            var result = _manager.GetActiveProviders();

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(p => p.ProviderType == ProviderType.Email);
            result.Should().NotContain(p => p.ProviderType == ProviderType.Sms);
        }

        [Fact]
        public void GetProvider_WithValidProviderType_ShouldReturnProvider()
        {
            // Act
            var result = _manager.GetProvider(ProviderType.Email);

            // Assert
            result.Should().NotBeNull();
            result.ProviderType.Should().Be(ProviderType.Email);
        }

        [Fact]
        public void GetProvider_WithInvalidProviderType_ShouldThrowArgumentException()
        {
            // Act & Assert
            var action = () => _manager.GetProvider(ProviderType.WebSocket);
            action.Should().Throw<ArgumentException>().WithMessage("*Provider not found for type: WebSocket*");
        }

        [Fact]
        public async Task CheckAllProvidersHealthAsync_WithHealthyProviders_ShouldReturnHealthyStatuses()
        {
            // Arrange
            var emailHealthStatus = new ProviderHealthStatus
            {
                IsHealthy = true,
                Status = "Operational",
                ResponseTime = TimeSpan.FromMilliseconds(100)
            };

            var smsHealthStatus = new ProviderHealthStatus
            {
                IsHealthy = true,
                Status = "Operational",
                ResponseTime = TimeSpan.FromMilliseconds(150)
            };

            _mockEmailProvider
                .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(emailHealthStatus);

            _mockSmsProvider
                .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(smsHealthStatus);

            // Act
            var result = await _manager.CheckAllProvidersHealthAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result[ProviderType.Email].Should().Be(emailHealthStatus);
            result[ProviderType.Sms].Should().Be(smsHealthStatus);
        }

        [Fact]
        public async Task CheckAllProvidersHealthAsync_WithProviderException_ShouldReturnErrorStatus()
        {
            // Arrange
            var exception = new InvalidOperationException("Provider error");

            _mockEmailProvider
                .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            var smsHealthStatus = new ProviderHealthStatus
            {
                IsHealthy = true,
                Status = "Operational",
                ResponseTime = TimeSpan.FromMilliseconds(150)
            };

            _mockSmsProvider
                .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(smsHealthStatus);

            // Act
            var result = await _manager.CheckAllProvidersHealthAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result[ProviderType.Email].IsHealthy.Should().BeFalse();
            result[ProviderType.Email].Status.Should().Be("Error");
            result[ProviderType.Email].ErrorMessage.Should().Be("Provider error");
            result[ProviderType.Email].ResponseTime.Should().Be(TimeSpan.Zero);

            result[ProviderType.Sms].Should().Be(smsHealthStatus);
        }

        [Fact]
        public async Task SendNotificationAsync_WithPreferredProviderAvailable_ShouldUsePreferredProvider()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            var expectedResult = new NotificationResult { Success = true };

            _mockEmailProvider
                .Setup(x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockEmailProvider
                .Setup(x => x.SendAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _manager.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResult);

            _mockEmailProvider.Verify(
                x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockEmailProvider.Verify(
                x => x.SendAsync(notification, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockSmsProvider.Verify(
                x => x.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SendNotificationAsync_WithPreferredProviderUnavailable_ShouldUseAlternativeProvider()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientPhoneNumber("+1234567890");
            
            // Disable email provider (preferred provider)
            _mockEmailProvider.Setup(x => x.IsEnabled).Returns(false);

            var expectedResult = new NotificationResult { Success = true };

            _mockSmsProvider
                .Setup(x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockSmsProvider
                .Setup(x => x.SendAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _manager.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResult);

            _mockSmsProvider.Verify(
                x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockSmsProvider.Verify(
                x => x.SendAsync(notification, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WithNoProvidersAvailable_ShouldReturnFailureResult()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            // Disable all providers
            _mockEmailProvider.Setup(x => x.IsEnabled).Returns(false);
            _mockSmsProvider.Setup(x => x.IsEnabled).Returns(false);

            // Act
            var result = await _manager.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("No suitable notification provider available");
        }

        [Fact]
        public async Task SendNotificationAsync_WithValidationFailure_ShouldReturnValidationError()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            _mockEmailProvider
                .Setup(x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _manager.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Notification validation failed for provider Email");

            _mockEmailProvider.Verify(
                x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockEmailProvider.Verify(
                x => x.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SendNotificationAsync_ShouldPreferHigherPriorityProvider()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            // Set email provider as preferred but disabled
            _mockEmailProvider.Setup(x => x.IsEnabled).Returns(false);

            // Set SMS provider priority higher than email
            _mockSmsProvider.Setup(x => x.Priority).Returns(NotificationPriority.Critical);

            var expectedResult = new NotificationResult { Success = true };

            _mockSmsProvider
                .Setup(x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockSmsProvider
                .Setup(x => x.SendAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _manager.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResult);

            _mockSmsProvider.Verify(
                x => x.SendAsync(notification, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_ShouldLogAppropriateMessages()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            var expectedResult = new NotificationResult { Success = true };

            _mockEmailProvider
                .Setup(x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockEmailProvider
                .Setup(x => x.SendAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            await _manager.SendNotificationAsync(notification, CancellationToken.None);

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
        public async Task SendNotificationAsync_WhenPreferredProviderUnavailable_ShouldLogWarning()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            // Disable email provider (preferred provider)
            _mockEmailProvider.Setup(x => x.IsEnabled).Returns(false);

            var expectedResult = new NotificationResult { Success = true };

            _mockSmsProvider
                .Setup(x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockSmsProvider
                .Setup(x => x.SendAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            await _manager.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Preferred provider") && v.ToString()!.Contains("not available")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WhenNoProvidersAvailable_ShouldLogError()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            // Disable all providers
            _mockEmailProvider.Setup(x => x.IsEnabled).Returns(false);
            _mockSmsProvider.Setup(x => x.IsEnabled).Returns(false);

            // Act
            await _manager.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No suitable provider found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WhenValidationFails_ShouldLogError()
        {
            // Arrange
            var notification = CreateTestNotification();
            notification.SetRecipientEmail("test@example.com");

            _mockEmailProvider
                .Setup(x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _manager.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("validation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CheckAllProvidersHealthAsync_WhenProviderThrowsException_ShouldLogError()
        {
            // Arrange
            var exception = new InvalidOperationException("Health check failed");

            _mockEmailProvider
                .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            var smsHealthStatus = new ProviderHealthStatus { IsHealthy = true };
            _mockSmsProvider
                .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(smsHealthStatus);

            // Act
            await _manager.CheckAllProvidersHealthAsync(CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error checking health of provider")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static Notification CreateTestNotification()
        {
            return new Notification("user123", "Test Subject", "Test Content", NotificationPriority.Normal, "corr-123");
        }
    }
}
