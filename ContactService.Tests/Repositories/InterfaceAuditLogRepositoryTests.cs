using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace ContactService.Tests.Repositories;

public class InterfaceAuditLogRepositoryTests
{
    private readonly Mock<IAuditLogRepository> _mockRepository;

    public InterfaceAuditLogRepositoryTests()
    {
        _mockRepository = new Mock<IAuditLogRepository>();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedAuditLog()
    {
        // Arrange
        var auditLog = new AuditLog(
            "test-correlation",
            "ContactService",
            "CREATE",
            "Contact",
            "contact-123");

        _mockRepository.Setup(r => r.CreateAsync(
                It.IsAny<AuditLog>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLog log, CancellationToken _) => log);

        // Act
        var result = await _mockRepository.Object.CreateAsync(auditLog);

        // Assert
        result.Should().NotBeNull();
        result.CorrelationId.Should().Be("test-correlation");
        result.Action.Should().Be("CREATE");
        result.EntityType.Should().Be("Contact");
        result.EntityId.Should().Be("contact-123");
        
        _mockRepository.Verify(r => r.CreateAsync(
            It.Is<AuditLog>(log => 
                log.CorrelationId == "test-correlation" && 
                log.Action == "CREATE"),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task CreateManyAsync_ShouldCallRepositoryWithAllLogs()
    {
        // Arrange
        var auditLogs = new List<AuditLog>
        {
            new AuditLog("corr-1", "ContactService", "CREATE", "Contact", "contact-1"),
            new AuditLog("corr-1", "ContactService", "UPDATE", "Contact", "contact-2")
        };

        _mockRepository.Setup(r => r.CreateManyAsync(
                It.IsAny<IEnumerable<AuditLog>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockRepository.Object.CreateManyAsync(auditLogs);

        // Assert
        _mockRepository.Verify(r => r.CreateManyAsync(
            It.Is<IEnumerable<AuditLog>>(logs => 
                logs == auditLogs),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
    
    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldReturnMatchingLogs()
    {
        // Arrange
        var correlationId = "test-correlation";
        var expectedLogs = new List<AuditLog>
        {
            new AuditLog(correlationId, "ContactService", "CREATE", "Contact", "contact-1"),
            new AuditLog(correlationId, "ContactService", "UPDATE", "Contact", "contact-1")
        };

        _mockRepository.Setup(r => r.GetByCorrelationIdAsync(
                correlationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _mockRepository.Object.GetByCorrelationIdAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedLogs);
        
        _mockRepository.Verify(r => r.GetByCorrelationIdAsync(
            correlationId,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task GetByEntityAsync_ShouldReturnMatchingLogs()
    {
        // Arrange
        var entityType = "Contact";
        var entityId = "contact-123";
        var expectedLogs = new List<AuditLog>
        {
            new AuditLog("corr-1", "ContactService", "CREATE", entityType, entityId),
            new AuditLog("corr-2", "ContactService", "UPDATE", entityType, entityId)
        };

        _mockRepository.Setup(r => r.GetByEntityAsync(
                entityType,
                entityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _mockRepository.Object.GetByEntityAsync(entityType, entityId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedLogs);
        
        _mockRepository.Verify(r => r.GetByEntityAsync(
            entityType,
            entityId,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task SearchAsync_ShouldReturnFilteredResults()
    {
        // Arrange
        var correlationId = "test-corr";
        var serviceName = "ContactService";
        var userId = "user-123";
        var action = "CREATE";
        var entityType = "Contact";
        var entityId = "contact-123";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;
        
        var expectedLogs = new List<AuditLog>
        {
            new AuditLog(correlationId, serviceName, action, entityType, entityId) { UserId = userId }
        };

        _mockRepository.Setup(r => r.SearchAsync(
                correlationId, 
                serviceName,
                userId,
                action,
                entityType,
                entityId,
                startTime,
                endTime,
                0,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _mockRepository.Object.SearchAsync(
            correlationId, 
            serviceName,
            userId,
            action,
            entityType,
            entityId,
            startTime,
            endTime,
            0,
            100);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expectedLogs);
        
        _mockRepository.Verify(r => r.SearchAsync(
            correlationId, 
            serviceName,
            userId,
            action,
            entityType,
            entityId,
            startTime,
            endTime,
            0,
            100,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task DeleteOldLogsAsync_ShouldReturnDeletedCount()
    {
        // Arrange
        var beforeDate = DateTime.UtcNow.AddDays(-30);
        
        _mockRepository.Setup(r => r.DeleteOldLogsAsync(
                beforeDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await _mockRepository.Object.DeleteOldLogsAsync(beforeDate);

        // Assert
        result.Should().Be(42);
        
        _mockRepository.Verify(r => r.DeleteOldLogsAsync(
            beforeDate,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnCount()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetTotalCountAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        // Act
        var result = await _mockRepository.Object.GetTotalCountAsync();

        // Assert
        result.Should().Be(100);
        
        _mockRepository.Verify(r => r.GetTotalCountAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task IsHealthyAsync_ShouldReturnRepositoryHealth()
    {
        // Arrange
        _mockRepository.Setup(r => r.IsHealthyAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
        
        _mockRepository.Verify(r => r.IsHealthyAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-7);
        var endTime = DateTime.UtcNow;
        
        var expectedStats = new Dictionary<string, long>
        {
            ["ContactService"] = 42,
            ["ReportService"] = 18
        };

        _mockRepository.Setup(r => r.GetStatisticsAsync(
                startTime,
                endTime,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _mockRepository.Object.GetStatisticsAsync(startTime, endTime);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedStats);
        
        _mockRepository.Verify(r => r.GetStatisticsAsync(
            startTime,
            endTime,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task GetActionStatisticsAsync_ShouldReturnActionCounts()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-7);
        var endTime = DateTime.UtcNow;
        
        var expectedStats = new Dictionary<string, long>
        {
            ["CREATE"] = 25,
            ["UPDATE"] = 15,
            ["DELETE"] = 10
        };

        _mockRepository.Setup(r => r.GetActionStatisticsAsync(
                startTime,
                endTime,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _mockRepository.Object.GetActionStatisticsAsync(startTime, endTime);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedStats);
        
        _mockRepository.Verify(r => r.GetActionStatisticsAsync(
            startTime,
            endTime,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
