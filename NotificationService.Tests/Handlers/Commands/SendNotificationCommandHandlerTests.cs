using System;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService.Tests.Handlers.Commands
{
    public class SendNotificationCommandHandlerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILogger<SendNotificationCommandHandler>> _mockLogger;
        private readonly SendNotificationCommandHandler _handler;

        public SendNotificationCommandHandlerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<SendNotificationCommandHandler>>();
            _handler = new SendNotificationCommandHandler(_mockNotificationService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ShouldCreateAndSendNotification()
        {
            // Arrange
            var request = new SendNotificationRequest
            {
                UserId = "user123",
                Subject = "Test Subject",
                Content = "Test Content",
                RecipientEmail = "test@example.com",
                Priority = NotificationPriority.High,
                PreferredProvider = ProviderType.Email,
                CorrelationId = "corr-123"
            };

            var command = new SendNotificationCommand(request);
            var notificationId = Guid.NewGuid();
            var mockResult = new Mock<INotificationResult>();
            mockResult.Setup(x => x.Success).Returns(true);

            _mockNotificationService
                .Setup(x => x.CreateNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notificationId);

            _mockNotificationService
                .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(notificationId.ToString());
            result.Data.Success.Should().BeTrue();

            _mockNotificationService.Verify(
                x => x.CreateNotificationAsync(
                    It.Is<Notification>(n => 
                        n.UserId == request.UserId &&
                        n.Subject == request.Subject &&
                        n.Content == request.Content &&
                        n.CorrelationId == request.CorrelationId
                    ), 
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockNotificationService.Verify(
                x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithAdditionalData_ShouldAddToNotification()
        {
            // Arrange
            var additionalData = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };

            var request = new SendNotificationRequest
            {
                UserId = "user123",
                Subject = "Test Subject",
                Content = "Test Content",
                RecipientEmail = "test@example.com",
                AdditionalData = additionalData,
                CorrelationId = "corr-123"
            };

            var command = new SendNotificationCommand(request);
            var notificationId = Guid.NewGuid();
            var mockResult = new Mock<INotificationResult>();
            mockResult.Setup(x => x.Success).Returns(true);

            _mockNotificationService
                .Setup(x => x.CreateNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notificationId);

            _mockNotificationService
                .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            _mockNotificationService.Verify(
                x => x.CreateNotificationAsync(
                    It.Is<Notification>(n => n.AdditionalDataReadOnly.Count == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenSendFails_ShouldReturnFailureResponse()
        {
            // Arrange
            var request = new SendNotificationRequest
            {
                UserId = "user123",
                Subject = "Test Subject",
                Content = "Test Content",
                RecipientEmail = "test@example.com",
                CorrelationId = "corr-123"
            };

            var command = new SendNotificationCommand(request);
            var notificationId = Guid.NewGuid();
            var mockResult = new Mock<INotificationResult>();
            mockResult.Setup(x => x.Success).Returns(false);
            mockResult.Setup(x => x.ErrorMessage).Returns("Send failed");

            _mockNotificationService
                .Setup(x => x.CreateNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notificationId);

            _mockNotificationService
                .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Command handled successfully
            result.Data.Should().NotBeNull();
            result.Data!.Success.Should().BeFalse(); // But notification send failed
            result.Data.ErrorMessage.Should().Be("Send failed");
        }

        [Fact]
        public async Task Handle_WhenExceptionThrown_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new SendNotificationRequest
            {
                UserId = "user123",
                Subject = "Test Subject",
                Content = "Test Content",
                RecipientEmail = "test@example.com",
                CorrelationId = "corr-123"
            };

            var command = new SendNotificationCommand(request);

            _mockNotificationService
                .Setup(x => x.CreateNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Test exception");
        }

        [Fact]
        public async Task Handle_WithPhoneNumberRecipient_ShouldSetCorrectProvider()
        {
            // Arrange
            var request = new SendNotificationRequest
            {
                UserId = "user123",
                Subject = "Test Subject",
                Content = "Test Content",
                RecipientPhoneNumber = "+1234567890",
                PreferredProvider = ProviderType.Sms,
                CorrelationId = "corr-123"
            };

            var command = new SendNotificationCommand(request);
            var notificationId = Guid.NewGuid();
            var mockResult = new Mock<INotificationResult>();
            mockResult.Setup(x => x.Success).Returns(true);

            _mockNotificationService
                .Setup(x => x.CreateNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notificationId);

            _mockNotificationService
                .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            _mockNotificationService.Verify(
                x => x.CreateNotificationAsync(
                    It.Is<Notification>(n => n.PreferredProvider == ProviderType.Sms),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithoutCorrelationId_ShouldGenerateNewCorrelationId()
        {
            // Arrange
            var request = new SendNotificationRequest
            {
                UserId = "user123",
                Subject = "Test Subject",
                Content = "Test Content",
                RecipientEmail = "test@example.com",
                CorrelationId = null // No correlation ID provided
            };

            var command = new SendNotificationCommand(request);
            var notificationId = Guid.NewGuid();
            var mockResult = new Mock<INotificationResult>();
            mockResult.Setup(x => x.Success).Returns(true);

            _mockNotificationService
                .Setup(x => x.CreateNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notificationId);

            _mockNotificationService
                .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            _mockNotificationService.Verify(
                x => x.CreateNotificationAsync(
                    It.Is<Notification>(n => !string.IsNullOrEmpty(n.CorrelationId)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
