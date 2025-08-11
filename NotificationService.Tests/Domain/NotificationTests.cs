using System;
using System.Collections.Generic;
using System.Linq;

namespace NotificationService.Tests.Domain
{
    public class NotificationTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateNotification()
        {
            // Arrange
            var userId = "user123";
            var subject = "Test Subject";
            var content = "Test Content";
            var priority = NotificationPriority.High;
            var correlationId = "corr-123";

            // Act
            var notification = new Notification(userId, subject, content, priority, correlationId);

            // Assert
            notification.UserId.Should().Be(userId);
            notification.Subject.Should().Be(subject);
            notification.Content.Should().Be(content);
            notification.Priority.Should().Be(priority);
            notification.CorrelationId.Should().Be(correlationId);
            notification.Id.Should().NotBeEmpty();
            notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            notification.IsDelivered.Should().BeFalse();
            notification.PreferredProvider.Should().Be(ProviderType.Unknown);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_WithInvalidUserId_ShouldThrowArgumentException(string? userId)
        {
            // Act & Assert
            var action = () => new Notification(userId!, "Subject", "Content", NotificationPriority.Normal, "corr-123");
            action.Should().Throw<ArgumentException>().WithMessage("*User ID cannot be null or empty*");
        }

        [Fact]
        public void Constructor_WithTooLongUserId_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = new string('a', 101);

            // Act & Assert
            var action = () => new Notification(userId, "Subject", "Content", NotificationPriority.Normal, "corr-123");
            action.Should().Throw<ArgumentException>().WithMessage("*User ID cannot exceed 100 characters*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_WithInvalidSubject_ShouldThrowArgumentException(string? subject)
        {
            // Act & Assert
            var action = () => new Notification("user123", subject!, "Content", NotificationPriority.Normal, "corr-123");
            action.Should().Throw<ArgumentException>().WithMessage("*Subject cannot be null or empty*");
        }

        [Fact]
        public void Constructor_WithTooLongSubject_ShouldThrowArgumentException()
        {
            // Arrange
            var subject = new string('a', 201);

            // Act & Assert
            var action = () => new Notification("user123", subject, "Content", NotificationPriority.Normal, "corr-123");
            action.Should().Throw<ArgumentException>().WithMessage("*Subject cannot exceed 200 characters*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_WithInvalidContent_ShouldThrowArgumentException(string? content)
        {
            // Act & Assert
            var action = () => new Notification("user123", "Subject", content!, NotificationPriority.Normal, "corr-123");
            action.Should().Throw<ArgumentException>().WithMessage("*Content cannot be null or empty*");
        }

        [Fact]
        public void Constructor_WithTooLongContent_ShouldThrowArgumentException()
        {
            // Arrange
            var content = new string('a', 2001);

            // Act & Assert
            var action = () => new Notification("user123", "Subject", content, NotificationPriority.Normal, "corr-123");
            action.Should().Throw<ArgumentException>().WithMessage("*Content cannot exceed 2000 characters*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_WithInvalidCorrelationId_ShouldThrowArgumentException(string? correlationId)
        {
            // Act & Assert
            var action = () => new Notification("user123", "Subject", "Content", NotificationPriority.Normal, correlationId!);
            action.Should().Throw<ArgumentException>().WithMessage("*Correlation ID cannot be null or empty*");
        }

        [Fact]
        public void SetRecipientEmail_WithValidEmail_ShouldSetEmailAndProvider()
        {
            // Arrange
            var notification = CreateValidNotification();
            var email = "test@example.com";

            // Act
            notification.SetRecipientEmail(email);

            // Assert
            notification.RecipientEmail.Should().Be(email.ToLowerInvariant());
            notification.PreferredProvider.Should().Be(ProviderType.Email);
            notification.HasRecipientEmail.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("invalid-email")]
        [InlineData("@example.com")]
        [InlineData("test@")]
        public void SetRecipientEmail_WithInvalidEmail_ShouldThrowArgumentException(string? email)
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act & Assert
            var action = () => notification.SetRecipientEmail(email!);
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetRecipientPhoneNumber_WithValidPhoneNumber_ShouldSetPhoneAndProvider()
        {
            // Arrange
            var notification = CreateValidNotification();
            var phoneNumber = "+1234567890";

            // Act
            notification.SetRecipientPhoneNumber(phoneNumber);

            // Assert
            notification.RecipientPhoneNumber.Should().Be(phoneNumber);
            notification.PreferredProvider.Should().Be(ProviderType.Sms);
            notification.HasRecipientPhoneNumber.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("123")]
        [InlineData("12345678901234567890")]
        public void SetRecipientPhoneNumber_WithInvalidPhoneNumber_ShouldThrowArgumentException(string? phoneNumber)
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act & Assert
            var action = () => notification.SetRecipientPhoneNumber(phoneNumber!);
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetPreferredProvider_WithValidProvider_ShouldSetProvider()
        {
            // Arrange
            var notification = CreateValidNotification();
            notification.SetRecipientEmail("test@example.com");

            // Act
            notification.SetPreferredProvider(ProviderType.Email);

            // Assert
            notification.PreferredProvider.Should().Be(ProviderType.Email);
        }

        [Fact]
        public void SetPreferredProvider_WithUnknownProvider_ShouldThrowArgumentException()
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act & Assert
            var action = () => notification.SetPreferredProvider(ProviderType.Unknown);
            action.Should().Throw<ArgumentException>().WithMessage("*Preferred provider cannot be Unknown*");
        }

        [Fact]
        public void SetPreferredProvider_EmailWithoutRecipientEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act & Assert
            var action = () => notification.SetPreferredProvider(ProviderType.Email);
            action.Should().Throw<InvalidOperationException>().WithMessage("*Cannot set Email provider without recipient email*");
        }

        [Fact]
        public void SetPreferredProvider_SmsWithoutRecipientPhone_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act & Assert
            var action = () => notification.SetPreferredProvider(ProviderType.Sms);
            action.Should().Throw<InvalidOperationException>().WithMessage("*Cannot set SMS provider without recipient phone number*");
        }

        [Fact]
        public void MarkAsSent_WithValidProvider_ShouldMarkAsSent()
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act
            notification.MarkAsSent(ProviderType.Email);

            // Assert
            notification.IsDelivered.Should().BeTrue();
            notification.HasBeenSent.Should().BeTrue();
            notification.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            notification.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void MarkAsSent_WhenAlreadySent_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var notification = CreateValidNotification();
            notification.MarkAsSent(ProviderType.Email);

            // Act & Assert
            var action = () => notification.MarkAsSent(ProviderType.Email);
            action.Should().Throw<InvalidOperationException>().WithMessage("*Notification has already been sent*");
        }

        [Fact]
        public void MarkAsSent_WithUnknownProvider_ShouldThrowArgumentException()
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act & Assert
            var action = () => notification.MarkAsSent(ProviderType.Unknown);
            action.Should().Throw<ArgumentException>().WithMessage("*Sent via provider cannot be Unknown*");
        }

        [Fact]
        public void MarkAsFailed_WithValidErrorMessage_ShouldMarkAsFailed()
        {
            // Arrange
            var notification = CreateValidNotification();
            var errorMessage = "Test error message";

            // Act
            notification.MarkAsFailed(errorMessage);

            // Assert
            notification.IsDelivered.Should().BeFalse();
            notification.ErrorMessage.Should().Be(errorMessage);
            notification.HasFailed.Should().BeTrue();
            notification.SentAt.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void MarkAsFailed_WithInvalidErrorMessage_ShouldThrowArgumentException(string? errorMessage)
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act & Assert
            var action = () => notification.MarkAsFailed(errorMessage!);
            action.Should().Throw<ArgumentException>().WithMessage("*Error message cannot be empty*");
        }

        [Fact]
        public void MarkAsFailed_WithTooLongErrorMessage_ShouldThrowArgumentException()
        {
            // Arrange
            var notification = CreateValidNotification();
            var errorMessage = new string('a', 1001);

            // Act & Assert
            var action = () => notification.MarkAsFailed(errorMessage);
            action.Should().Throw<ArgumentException>().WithMessage("*Error message cannot exceed 1000 characters*");
        }

        [Fact]
        public void AddAdditionalData_WithValidKeyValue_ShouldAddData()
        {
            // Arrange
            var notification = CreateValidNotification();
            var key = "testKey";
            var value = "testValue";

            // Act
            notification.AddAdditionalData(key, value);

            // Assert
            notification.AdditionalDataReadOnly.Should().ContainKey(key);
            notification.AdditionalDataReadOnly[key].Should().Be(value);
        }

        [Theory]
        [InlineData("", "value")]
        [InlineData("   ", "value")]
        [InlineData(null, "value")]
        [InlineData("key", "")]
        [InlineData("key", "   ")]
        [InlineData("key", null)]
        public void AddAdditionalData_WithInvalidKeyOrValue_ShouldThrowArgumentException(string? key, string? value)
        {
            // Arrange
            var notification = CreateValidNotification();

            // Act & Assert
            var action = () => notification.AddAdditionalData(key!, value!);
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddAdditionalData_WithTooLongKey_ShouldThrowArgumentException()
        {
            // Arrange
            var notification = CreateValidNotification();
            var key = new string('a', 101);

            // Act & Assert
            var action = () => notification.AddAdditionalData(key, "value");
            action.Should().Throw<ArgumentException>().WithMessage("*Key cannot exceed 100 characters*");
        }

        [Fact]
        public void AddAdditionalData_WithTooLongValue_ShouldThrowArgumentException()
        {
            // Arrange
            var notification = CreateValidNotification();
            var value = new string('a', 501);

            // Act & Assert
            var action = () => notification.AddAdditionalData("key", value);
            action.Should().Throw<ArgumentException>().WithMessage("*Value cannot exceed 500 characters*");
        }

        [Fact]
        public void RemoveAdditionalData_WithExistingKey_ShouldRemoveData()
        {
            // Arrange
            var notification = CreateValidNotification();
            var key = "testKey";
            notification.AddAdditionalData(key, "testValue");

            // Act
            notification.RemoveAdditionalData(key);

            // Assert
            notification.AdditionalDataReadOnly.Should().NotContainKey(key);
        }

        [Fact]
        public void UpdatePriority_WhenNotSent_ShouldUpdatePriority()
        {
            // Arrange
            var notification = CreateValidNotification();
            var newPriority = NotificationPriority.Critical;

            // Act
            notification.UpdatePriority(newPriority);

            // Assert
            notification.Priority.Should().Be(newPriority);
        }

        [Fact]
        public void UpdatePriority_WhenAlreadySent_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var notification = CreateValidNotification();
            notification.MarkAsSent(ProviderType.Email);

            // Act & Assert
            var action = () => notification.UpdatePriority(NotificationPriority.Critical);
            action.Should().Throw<InvalidOperationException>().WithMessage("*Cannot update priority of sent notification*");
        }

        [Fact]
        public void UpdateContent_WhenNotSent_ShouldUpdateContent()
        {
            // Arrange
            var notification = CreateValidNotification();
            var newSubject = "New Subject";
            var newContent = "New Content";

            // Act
            notification.UpdateContent(newSubject, newContent);

            // Assert
            notification.Subject.Should().Be(newSubject);
            notification.Content.Should().Be(newContent);
        }

        [Fact]
        public void UpdateContent_WhenAlreadySent_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var notification = CreateValidNotification();
            notification.MarkAsSent(ProviderType.Email);

            // Act & Assert
            var action = () => notification.UpdateContent("New Subject", "New Content");
            action.Should().Throw<InvalidOperationException>().WithMessage("*Cannot update content of sent notification*");
        }

        [Theory]
        [InlineData(NotificationPriority.High, true)]
        [InlineData(NotificationPriority.Critical, true)]
        [InlineData(NotificationPriority.Normal, false)]
        [InlineData(NotificationPriority.Low, false)]
        public void IsHighPriority_ShouldReturnCorrectValue(NotificationPriority priority, bool expected)
        {
            // Arrange
            var notification = new Notification("user123", "Subject", "Content", priority, "corr-123");

            // Assert
            notification.IsHighPriority.Should().Be(expected);
        }

        [Fact]
        public void HasValidRecipient_WithEmail_ShouldReturnTrue()
        {
            // Arrange
            var notification = CreateValidNotification();
            notification.SetRecipientEmail("test@example.com");

            // Assert
            notification.HasValidRecipient().Should().BeTrue();
        }

        [Fact]
        public void HasValidRecipient_WithPhoneNumber_ShouldReturnTrue()
        {
            // Arrange
            var notification = CreateValidNotification();
            notification.SetRecipientPhoneNumber("+1234567890");

            // Assert
            notification.HasValidRecipient().Should().BeTrue();
        }

        [Fact]
        public void HasValidRecipient_WithoutRecipient_ShouldReturnFalse()
        {
            // Arrange
            var notification = CreateValidNotification();

            // Assert
            notification.HasValidRecipient().Should().BeFalse();
        }

        private static Notification CreateValidNotification()
        {
            return new Notification("user123", "Test Subject", "Test Content", NotificationPriority.Normal, "corr-123");
        }
    }
}
