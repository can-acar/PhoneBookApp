using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Services;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Moq;
using Xunit;

namespace ContactService.Tests.MongoDb
{
    public class MongoAuditLogRepositoryStatisticsTests
    {
        private readonly Mock<IMongoCollection<AuditLog>> _mockCollection;
        private readonly Mock<IMongoDbContext> _mockContext;
        private readonly MongoAuditLogRepository _repository;

        public MongoAuditLogRepositoryStatisticsTests()
        {
            _mockCollection = new Mock<IMongoCollection<AuditLog>>();
            _mockContext = new Mock<IMongoDbContext>();
            _mockContext.Setup(c => c.GetCollection<AuditLog>(It.IsAny<string>())).Returns(_mockCollection.Object);
            _repository = new MongoAuditLogRepository(_mockContext.Object);
        }

        [Fact]
        public async Task GetStatisticsAsync_ShouldAggregateByServiceName()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-7);
            var endTime = DateTime.UtcNow;
            
            // Mock pipeline execution result
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            var resultList = new List<BsonDocument>
            {
                new BsonDocument
                {
                    { "_id", "ContactService" },
                    { "count", 42 }
                },
                new BsonDocument
                {
                    { "_id", "ReportService" },
                    { "count", 18 }
                }
            };

            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(c => c.Current).Returns(resultList);

            // Configure collection to return our mocked cursor when aggregate is called with any pipeline
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
            result.Should().ContainKey("ContactService").WhoseValue.Should().Be(42);
            result.Should().ContainKey("ReportService").WhoseValue.Should().Be(18);
        }

        [Fact]
        public async Task GetActionStatisticsAsync_ShouldAggregateByAction()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-7);
            var endTime = DateTime.UtcNow;
            
            // Mock pipeline execution result
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            var resultList = new List<BsonDocument>
            {
                new BsonDocument
                {
                    { "_id", "CREATE" },
                    { "count", 25 }
                },
                new BsonDocument
                {
                    { "_id", "UPDATE" },
                    { "count", 15 }
                },
                new BsonDocument
                {
                    { "_id", "DELETE" },
                    { "count", 10 }
                }
            };

            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(c => c.Current).Returns(resultList);

            // Configure collection to return our mocked cursor when aggregate is called with any pipeline
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
            result.Should().ContainKey("CREATE").WhoseValue.Should().Be(25);
            result.Should().ContainKey("UPDATE").WhoseValue.Should().Be(15);
            result.Should().ContainKey("DELETE").WhoseValue.Should().Be(10);
        }

        [Fact]
        public async Task GetStatisticsAsync_WithNullParameters_ShouldReturnAllStats()
        {
            // Arrange
            // Mock pipeline execution result
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            var resultList = new List<BsonDocument>
            {
                new BsonDocument
                {
                    { "_id", "ContactService" },
                    { "count", 100 }
                },
                new BsonDocument
                {
                    { "_id", "NotificationService" },
                    { "count", 50 }
                },
                new BsonDocument
                {
                    { "_id", "ReportService" },
                    { "count", 25 }
                }
            };

            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(c => c.Current).Returns(resultList);

            // Configure collection to return our mocked cursor when aggregate is called with any pipeline
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
            result.Should().ContainKey("ContactService").WhoseValue.Should().Be(100);
            result.Should().ContainKey("NotificationService").WhoseValue.Should().Be(50);
            result.Should().ContainKey("ReportService").WhoseValue.Should().Be(25);
        }

        [Fact]
        public async Task GetActionStatisticsAsync_WithNullParameters_ShouldReturnAllActionStats()
        {
            // Arrange
            // Mock pipeline execution result
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            var resultList = new List<BsonDocument>
            {
                new BsonDocument
                {
                    { "_id", "CREATE" },
                    { "count", 50 }
                },
                new BsonDocument
                {
                    { "_id", "UPDATE" },
                    { "count", 30 }
                },
                new BsonDocument
                {
                    { "_id", "DELETE" },
                    { "count", 20 }
                }
            };

            mockCursor
                .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(c => c.Current).Returns(resultList);

            // Configure collection to return our mocked cursor when aggregate is called with any pipeline
            _mockCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<AuditLog, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetActionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().ContainKey("CREATE").WhoseValue.Should().Be(50);
            result.Should().ContainKey("UPDATE").WhoseValue.Should().Be(30);
            result.Should().ContainKey("DELETE").WhoseValue.Should().Be(20);
        }
    }
}
