using System;
using System.Collections.Generic;
using System.Text.Json;
using ContactService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ContactService.Tests.Domain.Entities
{
    public class ContactHistoryTests
    {
        [Fact]
        public void Constructor_WithValidParams_ShouldCreateHistoryRecord()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var operationType = ContactHistoryOperationType.CREATE;
            var contactData = new { FirstName = "John", LastName = "Doe" };
            var correlationId = "test-correlation-id";
            var userId = "test-user";
            var ipAddress = "127.0.0.1";
            var userAgent = "test-agent";
            var additionalMetadata = new Dictionary<string, object> { ["source"] = "test" };

            // Act
            var history = new ContactHistory(
                contactId,
                operationType,
                contactData,
                correlationId,
                userId,
                ipAddress,
                userAgent,
                additionalMetadata);

            // Assert
            history.Should().NotBeNull();
            history.Id.Should().NotBe(Guid.Empty);
            history.ContactId.Should().Be(contactId);
            history.OperationType.Should().Be(operationType);
            history.CorrelationId.Should().Be(correlationId);
            history.UserId.Should().Be(userId);
            history.IPAddress.Should().Be(ipAddress);
            history.UserAgent.Should().Be(userAgent);
            history.AdditionalMetadata.Should().NotBeNullOrWhiteSpace();
            history.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Constructor_WithEmptyContactId_ShouldThrowException()
        {
            // Arrange
            var contactId = Guid.Empty;
            var operationType = ContactHistoryOperationType.CREATE;
            var contactData = new { FirstName = "John", LastName = "Doe" };
            var correlationId = "test-correlation-id";

            // Act & Assert
            Action act = () => new ContactHistory(contactId, operationType, contactData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Contact ID cannot be empty*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_WithInvalidOperationType_ShouldThrowException(string operationType)
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var contactData = new { FirstName = "John", LastName = "Doe" };
            var correlationId = "test-correlation-id";

            // Act & Assert
            Action act = () => new ContactHistory(contactId, operationType, contactData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Operation type cannot be null or empty*");
        }


        [Fact]
        public void Constructor_WithNullContactData_ShouldThrowException()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var operationType = ContactHistoryOperationType.CREATE;
            object contactData = null;
            var correlationId = "test-correlation-id";

            // Act & Assert
            Action act = () => new ContactHistory(contactId, operationType, contactData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Contact data cannot be null*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_WithInvalidCorrelationId_ShouldThrowException(string correlationId)
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var operationType = ContactHistoryOperationType.CREATE;
            var contactData = new { FirstName = "John", LastName = "Doe" };

            // Act & Assert
            Action act = () => new ContactHistory(contactId, operationType, contactData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Correlation ID cannot be null or empty*");
        }

        [Fact]
        public void Constructor_WithTooLongCorrelationId_ShouldThrowException()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var operationType = ContactHistoryOperationType.CREATE;
            var contactData = new { FirstName = "John", LastName = "Doe" };
            var correlationId = new string('a', 101); // 101 characters

            // Act & Assert
            Action act = () => new ContactHistory(contactId, operationType, contactData, correlationId);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Correlation ID cannot exceed 100 characters*");
        }

        [Fact]
        public void GetContactData_WithValidData_ShouldDeserializeCorrectly()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var operationType = ContactHistoryOperationType.CREATE;
            var contactData = new TestContactData { FirstName = "John", LastName = "Doe" };
            var correlationId = "test-correlation-id";

            var history = new ContactHistory(contactId, operationType, contactData, correlationId);

            // Act
            var deserializedData = history.GetContactData<TestContactData>();

            // Assert
            deserializedData.Should().NotBeNull();
            deserializedData.FirstName.Should().Be(contactData.FirstName);
            deserializedData.LastName.Should().Be(contactData.LastName);
        }

        [Fact]
        public void GetAdditionalMetadata_WithValidData_ShouldDeserializeCorrectly()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var operationType = ContactHistoryOperationType.CREATE;
            var contactData = new { FirstName = "John", LastName = "Doe" };
            var correlationId = "test-correlation-id";
            var additionalMetadata = new Dictionary<string, object>
            {
                ["source"] = "test",
                ["version"] = 1
            };

            var history = new ContactHistory(contactId, operationType, contactData, correlationId, 
                additionalMetadata: additionalMetadata);

            // Act
            var deserializedMetadata = history.GetAdditionalMetadata();

            // Assert
            deserializedMetadata.Should().NotBeNull();
            deserializedMetadata.Should().ContainKey("source");
            JsonSerializer.Deserialize<string>(((JsonElement)deserializedMetadata["source"]).GetRawText())
                .Should().Be("test");
        }

        [Fact]
        public void GetAdditionalMetadata_WithNoMetadata_ShouldReturnNull()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var operationType = ContactHistoryOperationType.CREATE;
            var contactData = new { FirstName = "John", LastName = "Doe" };
            var correlationId = "test-correlation-id";

            var history = new ContactHistory(contactId, operationType, contactData, correlationId);

            // Act
            var deserializedMetadata = history.GetAdditionalMetadata();

            // Assert
            deserializedMetadata.Should().BeNull();
        }

        private class TestContactData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}
