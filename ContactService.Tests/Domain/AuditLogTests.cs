using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace ContactService.Tests.Domain;

public class AuditLogTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var action = "CREATE";
        var entityType = "Contact";
        var entityId = Guid.NewGuid().ToString(); // string olarak
        var userId = "user123";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var auditLog = new AuditLog
        {
            Id = ObjectId.GenerateNewId().ToString(), // string olarak
            Timestamp = timestamp,
            Action = action,
            EntityType = entityType,
            EntityId = entityId, // string
            UserId = userId,
            CorrelationId = correlationId,
            NewValues = new { firstName = "John" }, // Changes yerine NewValues
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Assert
        auditLog.Id.Should().NotBeNullOrEmpty(); // string kontrolü
        auditLog.Timestamp.Should().Be(timestamp);
        auditLog.Action.Should().Be(action);
        auditLog.EntityType.Should().Be(entityType);
        auditLog.EntityId.Should().Be(entityId); // string karşılaştırması
        auditLog.UserId.Should().Be(userId);
        auditLog.CorrelationId.Should().Be(correlationId);
        auditLog.NewValues.Should().NotBeNull(); // Changes yerine NewValues
        auditLog.IpAddress.Should().Be("192.168.1.1");
        auditLog.UserAgent.Should().Be("Mozilla/5.0");
    }

    [Fact]
    public void AuditLog_Properties_ShouldBeSettable()
    {
        // Arrange
        var auditLog = new AuditLog();

        // Act
        auditLog.Action = "UPDATE";
        auditLog.EntityType = "ContactInfo";
        auditLog.NewValues = new { infoValue = "new-email@test.com" }; // Changes yerine NewValues

        // Assert
        auditLog.Action.Should().Be("UPDATE");
        auditLog.EntityType.Should().Be("ContactInfo");
        auditLog.NewValues.Should().NotBeNull(); // Changes yerine NewValues kontrolü
    }
}
