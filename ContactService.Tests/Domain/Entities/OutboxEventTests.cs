using System;
using System.Text.Json;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ContactService.Tests.Domain.Entities
{
    public class OutboxEventTests
    {
        [Fact]
        public void Constructor_WithValidParams_ShouldCreateOutboxEvent()
        {
            // Arrange
            var eventType = "ContactCreated";
            var eventData = new { ContactId = Guid.NewGuid(), Name = "Test Contact" };
            var correlationId = "test-correlation-id";

            // Act
            var outboxEvent = new OutboxEvent(eventType, eventData, correlationId);

            // Assert
            outboxEvent.Should().NotBeNull();
            outboxEvent.Id.Should().NotBe(Guid.Empty);
            outboxEvent.EventType.Should().Be(eventType);
            outboxEvent.CorrelationId.Should().Be(correlationId);
            outboxEvent.Status.Should().Be(OutboxEventStatus.Pending);
            outboxEvent.RetryCount.Should().Be(0);
            outboxEvent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            outboxEvent.ProcessedAt.Should().BeNull();
            outboxEvent.ErrorMessage.Should().BeNull();
            outboxEvent.NextRetryAt.Should().BeNull();
            outboxEvent.IsPending.Should().BeTrue();
            outboxEvent.IsProcessed.Should().BeFalse();
            outboxEvent.IsFailed.Should().BeFalse();
            outboxEvent.CanRetry.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_WithInvalidEventType_ShouldThrowException(string eventType)
        {
            // Arrange
            var eventData = new { ContactId = Guid.NewGuid(), Name = "Test Contact" };
            var correlationId = "test-correlation-id";

            // Act & Assert
            Action act = () => new OutboxEvent(eventType, eventData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Event type cannot be null or empty*");
        }

        [Fact]
        public void Constructor_WithTooLongEventType_ShouldThrowException()
        {
            // Arrange
            var eventType = new string('a', 101); // 101 characters
            var eventData = new { ContactId = Guid.NewGuid(), Name = "Test Contact" };
            var correlationId = "test-correlation-id";

            // Act & Assert
            Action act = () => new OutboxEvent(eventType, eventData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Event type cannot exceed 100 characters*");
        }

        [Fact]
        public void Constructor_WithNullEventData_ShouldThrowException()
        {
            // Arrange
            var eventType = "ContactCreated";
            object eventData = null;
            var correlationId = "test-correlation-id";

            // Act & Assert
            Action act = () => new OutboxEvent(eventType, eventData, correlationId);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*Event data cannot be null*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_WithInvalidCorrelationId_ShouldThrowException(string correlationId)
        {
            // Arrange
            var eventType = "ContactCreated";
            var eventData = new { ContactId = Guid.NewGuid(), Name = "Test Contact" };

            // Act & Assert
            Action act = () => new OutboxEvent(eventType, eventData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Correlation ID cannot be null or empty*");
        }

        [Fact]
        public void Constructor_WithTooLongCorrelationId_ShouldThrowException()
        {
            // Arrange
            var eventType = "ContactCreated";
            var eventData = new { ContactId = Guid.NewGuid(), Name = "Test Contact" };
            var correlationId = new string('a', 101); // 101 characters

            // Act & Assert
            Action act = () => new OutboxEvent(eventType, eventData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Correlation ID cannot exceed 100 characters*");
        }

        [Fact]
        public void MarkAsProcessed_WhenPending_ShouldUpdateStatus()
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "test-correlation-id");
            
            // Act
            outboxEvent.MarkAsProcessed();

            // Assert
            outboxEvent.Status.Should().Be(OutboxEventStatus.Processed);
            outboxEvent.ProcessedAt.Should().NotBeNull();
            outboxEvent.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            outboxEvent.ErrorMessage.Should().BeNull();
            outboxEvent.NextRetryAt.Should().BeNull();
            outboxEvent.IsProcessed.Should().BeTrue();
            outboxEvent.IsPending.Should().BeFalse();
            outboxEvent.IsFailed.Should().BeFalse();
        }

        [Fact]
        public void MarkAsProcessed_WhenNotPending_ShouldThrowException()
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "test-correlation-id");
            outboxEvent.MarkAsProcessed(); // First mark as processed

            // Act & Assert
            Action act = () => outboxEvent.MarkAsProcessed();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark event as processed from status: Processed");
        }

        [Fact]
        public void MarkAsFailed_WithValidErrorMessage_ShouldUpdateStatus()
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "test-correlation-id");
            var errorMessage = "Test error message";

            // Act
            outboxEvent.MarkAsFailed(errorMessage);

            // Assert
            outboxEvent.Status.Should().Be(OutboxEventStatus.Pending); // Since CanRetry is true
            outboxEvent.ErrorMessage.Should().Be(errorMessage);
            outboxEvent.RetryCount.Should().Be(1);
            outboxEvent.NextRetryAt.Should().NotBeNull();
            outboxEvent.NextRetryAt.Should().BeAfter(DateTime.UtcNow);
            outboxEvent.IsPending.Should().BeTrue();
            outboxEvent.IsProcessed.Should().BeFalse();
            outboxEvent.IsFailed.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void MarkAsFailed_WithInvalidErrorMessage_ShouldThrowException(string errorMessage)
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "test-correlation-id");

            // Act & Assert
            Action act = () => outboxEvent.MarkAsFailed(errorMessage);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Error message cannot be empty*");
        }

        [Fact]
        public void MarkAsFailed_WithTooLongErrorMessage_ShouldThrowException()
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "test-correlation-id");
            var errorMessage = new string('a', 1001); // 1001 characters

            // Act & Assert
            Action act = () => outboxEvent.MarkAsFailed(errorMessage);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Error message cannot exceed 1000 characters*");
        }

        [Fact]
        public void MarkAsFailed_AfterMaxRetries_ShouldSetStatusToFailed()
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "test-correlation-id");
            
            // Act: Mark as failed 5 times to exceed retry limit
            for (int i = 0; i < 4; i++)
            {
                outboxEvent.MarkAsFailed("Test error");
                // İlk 4 deneme için durum Pending olmalı
                outboxEvent.Status.Should().Be(OutboxEventStatus.Pending);
            }
            
            // Son hata işaretlemesi
            outboxEvent.MarkAsFailed("Final error");
            
            // Assert: 5. denemeden sonra Failed durumuna geçmeli
            outboxEvent.Status.Should().Be(OutboxEventStatus.Failed);
            outboxEvent.RetryCount.Should().Be(5);
            outboxEvent.ErrorMessage.Should().Be("Final error");
            outboxEvent.IsFailed.Should().BeTrue();
            outboxEvent.CanRetry.Should().BeFalse();
        }

        [Fact]
        public void ResetForRetry_WhenCanRetry_ShouldResetPendingStatus()
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "test-correlation-id");
            outboxEvent.MarkAsFailed("Test error");
            var nextRetryAt = outboxEvent.NextRetryAt;

            // Act
            outboxEvent.ResetForRetry();

            // Assert
            outboxEvent.Status.Should().Be(OutboxEventStatus.Pending);
            outboxEvent.NextRetryAt.Should().BeNull();
            outboxEvent.RetryCount.Should().Be(1); // Retry count remains the same
            outboxEvent.IsPending.Should().BeTrue();
        }

        [Fact]
        public void ResetForRetry_WhenCannotRetry_ShouldThrowException()
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "test-correlation-id");
            
            // Force to max retry count
            for (int i = 0; i < 6; i++) 
            {
                outboxEvent.MarkAsFailed("Test error");
            }

            // Act & Assert
            Action act = () => outboxEvent.ResetForRetry();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Event cannot be retried");
        }

        [Fact]
        public void GetEventData_WithValidData_ShouldDeserializeCorrectly()
        {
            // Arrange
            var eventData = new TestEventData { Id = Guid.NewGuid(), Name = "Test" };
            var outboxEvent = new OutboxEvent("TestEvent", eventData, "test-correlation-id");

            // Act
            var deserializedData = outboxEvent.GetEventData<TestEventData>();

            // Assert
            deserializedData.Should().NotBeNull();
            deserializedData.Id.Should().Be(eventData.Id);
            deserializedData.Name.Should().Be(eventData.Name);
        }

        private class TestEventData
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}
