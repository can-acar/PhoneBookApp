using ContactService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ContactService.Tests.Domain;

public class ContactHistoryTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateInstance()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var operationType = "CREATE";
        var contactData = new { firstName = "John", lastName = "Doe" };
        var correlationId = Guid.NewGuid().ToString();
        var userId = "user123";

        // Act
        var history = new ContactHistory(contactId, operationType, contactData, correlationId, userId);

        // Assert
        history.Should().NotBeNull();
    }

    [Theory]
    [InlineData("CREATE")]
    [InlineData("UPDATE")]
    [InlineData("DELETE")]
    public void Constructor_ShouldAcceptValidOperationTypes(string operationType)
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactData = new { firstName = "John", lastName = "Doe" };
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var history = new ContactHistory(contactId, operationType, contactData, correlationId);

        // Assert
        history.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateInstance()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var operationType = "UPDATE";
        var contactData = new { firstName = "Jane", lastName = "Smith", company = "TechCorp" };
        var correlationId = Guid.NewGuid().ToString();
        var userId = "admin";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var additionalMetadata = new Dictionary<string, object> { { "source", "api" } };

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
    }
}
