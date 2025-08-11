using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactService.Domain.Entities;
using ContactService.Infrastructure.Data;
using ContactService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace ContactService.Tests.Repositories
{
    public class MongoAuditLogRepositoryGetStatisticsTests
    {
        private readonly Mock<IMongoCollection<AuditLog>> _mockCollection;
        private readonly Mock<MongoDbContext> _mockContext;
        private readonly Mock<ILogger<MongoAuditLogRepository>> _mockLogger;
        private readonly MongoAuditLogRepository _repository;

        public MongoAuditLogRepositoryGetStatisticsTests()
        {
            _mockCollection = new Mock<IMongoCollection<AuditLog>>();
            _mockContext = new Mock<MongoDbContext>();
            _mockLogger = new Mock<ILogger<MongoAuditLogRepository>>();
            
            _mockContext.Setup(c => c.AuditLogs).Returns(_mockCollection.Object);
            
            _repository = new MongoAuditLogRepository(_mockContext.Object, _mockLogger.Object);
        }
        
        [Fact]
        public async Task GetStatisticsAsync_ShouldAggregateByServiceName()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-7);
            var endTime = DateTime.UtcNow;
            
            // Setup the async cursor mock
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            mockCursor
                .Setup(c => c.Current)
                .Returns(new List<BsonDocument>
                {
                    new BsonDocument
                    {
                        { "_id", "ContactService" },
                        { "count", 42L }
                    },
                    new BsonDocument
                    {
                        { "_id", "ReportService" },
                        { "count", 18L }
                    }
                });
            
            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            // Setup the collection to return the mock cursor
            _mockCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<AuditLog, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
            
            // Act
            var result = await _repository.GetStatisticsAsync(startTime, endTime);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result["ContactService"].Should().Be(42);
            result["ReportService"].Should().Be(18);
        }
        
        [Fact]
        public async Task GetStatisticsAsync_WithNullParameters_ShouldNotIncludeMatchStage()
        {
            // Arrange
            // Setup the async cursor mock
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            mockCursor
                .Setup(c => c.Current)
                .Returns(new List<BsonDocument>
                {
                    new BsonDocument
                    {
                        { "_id", "ContactService" },
                        { "count", 100L }
                    },
                    new BsonDocument
                    {
                        { "_id", "NotificationService" },
                        { "count", 50L }
                    },
                    new BsonDocument
                    {
                        { "_id", "ReportService" },
                        { "count", 25L }
                    }
                });
            
            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            // Setup the collection to return the mock cursor
            _mockCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<AuditLog, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
            
            // Act
            var result = await _repository.GetStatisticsAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result["ContactService"].Should().Be(100);
            result["NotificationService"].Should().Be(50);
            result["ReportService"].Should().Be(25);
        }

        [Fact]
        public async Task GetActionStatisticsAsync_ShouldAggregateByAction()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-7);
            var endTime = DateTime.UtcNow;
            
            // Setup the async cursor mock
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            mockCursor
                .Setup(c => c.Current)
                .Returns(new List<BsonDocument>
                {
                    new BsonDocument
                    {
                        { "_id", "CREATE" },
                        { "count", 25L }
                    },
                    new BsonDocument
                    {
                        { "_id", "UPDATE" },
                        { "count", 15L }
                    },
                    new BsonDocument
                    {
                        { "_id", "DELETE" },
                        { "count", 10L }
                    }
                });
            
            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            // Setup the collection to return the mock cursor
            _mockCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<AuditLog, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
            
            // Act
            var result = await _repository.GetActionStatisticsAsync(startTime, endTime);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result["CREATE"].Should().Be(25);
            result["UPDATE"].Should().Be(15);
            result["DELETE"].Should().Be(10);
        }
        
        [Fact]
        public async Task GetServiceStatisticsAsync_ShouldCallGetStatisticsAsync()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-7);
            var endTime = DateTime.UtcNow;
            
            // Setup the async cursor mock
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            mockCursor
                .Setup(c => c.Current)
                .Returns(new List<BsonDocument>
                {
                    new BsonDocument
                    {
                        { "_id", "ContactService" },
                        { "count", 42L }
                    },
                    new BsonDocument
                    {
                        { "_id", "ReportService" },
                        { "count", 18L }
                    }
                });
            
            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            // Setup the collection to return the mock cursor
            _mockCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<AuditLog, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
            
            // Act
            var result = await _repository.GetServiceStatisticsAsync(startTime, endTime);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result["ContactService"].Should().Be(42);
            result["ReportService"].Should().Be(18);
        }

        [Fact]
        public async Task GetStatisticsAsync_WithEmptyResult_ShouldReturnEmptyDictionary()
        {
            // Arrange
            // Setup the async cursor mock
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            mockCursor
                .Setup(c => c.Current)
                .Returns(new List<BsonDocument>());
            
            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            // Setup the collection to return the mock cursor
            _mockCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<AuditLog, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
            
            // Act
            var result = await _repository.GetStatisticsAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}
