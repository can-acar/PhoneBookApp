using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactService.ApplicationService.Services;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContactService.Tests.Services
{
    public class OutboxServiceTests
    {
        private readonly Mock<IOutboxRepository> _mockRepository;
        private readonly Mock<IKafkaProducer> _mockProducer;
        private readonly Mock<IOptions<KafkaSettings>> _mockKafkaOptions;
        private readonly Mock<ILogger<OutboxService>> _mockLogger;
        private readonly OutboxService _service;
        private readonly KafkaSettings _kafkaSettings;

        public OutboxServiceTests()
        {
            _mockRepository = new Mock<IOutboxRepository>();
            _mockProducer = new Mock<IKafkaProducer>();
            _mockLogger = new Mock<ILogger<OutboxService>>();
            
            _kafkaSettings = new KafkaSettings
            {
                BootstrapServers = "localhost:9092",
                GroupId = "test-group",
                Topics = new KafkaTopics
                {
                    ContactEvents = "contact-events",
                    ReportEvents = "report-events",
                    NotificationEvents = "notification-events"
                }
            };
            
            _mockKafkaOptions = new Mock<IOptions<KafkaSettings>>();
            _mockKafkaOptions.Setup(o => o.Value).Returns(_kafkaSettings);
            
            _service = new OutboxService(
                _mockRepository.Object,
                _mockProducer.Object,
                _mockKafkaOptions.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task AddEventAsync_ShouldCreateOutboxEvent()
        {
            // Arrange
            var eventType = "ContactCreated";
            var eventData = new { Id = Guid.NewGuid(), Name = "Test Contact" };
            var correlationId = "test-correlation-id";
            
            _mockRepository
                .Setup(r => r.CreateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OutboxEvent e, CancellationToken _) => e);

            // Act
            await _service.AddEventAsync(eventType, eventData, correlationId);

            // Assert
            _mockRepository.Verify(
                r => r.CreateAsync(
                    It.Is<OutboxEvent>(e => 
                        e.EventType == eventType && 
                        e.CorrelationId == correlationId), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact]
        public async Task ProcessPendingEventsAsync_WithPendingEvents_ShouldProcessEach()
        {
            // Arrange
            var pendingEvents = new List<OutboxEvent>
            {
                new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "correlation-1"),
                new OutboxEvent("ContactUpdated", new { Id = Guid.NewGuid() }, "correlation-2")
            };
            
            _mockRepository
                .Setup(r => r.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingEvents);
                
            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OutboxEvent e, CancellationToken _) => e);
                
            // Act
            await _service.ProcessPendingEventsAsync();
            
            // Assert
            _mockRepository.Verify(r => r.GetPendingEventsAsync(50, It.IsAny<CancellationToken>()), Times.Once);
            _mockProducer.Verify(
                p => p.PublishAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Exactly(2));
            _mockRepository.Verify(
                r => r.UpdateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()), 
                Times.Exactly(2));
        }
        
        [Fact]
        public async Task ProcessPendingEventsAsync_WithNoPendingEvents_ShouldDoNothing()
        {
            // Arrange
            _mockRepository
                .Setup(r => r.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxEvent>());
                
            // Act
            await _service.ProcessPendingEventsAsync();
            
            // Assert
            _mockRepository.Verify(r => r.GetPendingEventsAsync(50, It.IsAny<CancellationToken>()), Times.Once);
            _mockProducer.Verify(
                p => p.PublishAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Never);
            _mockRepository.Verify(
                r => r.UpdateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }
        
        [Fact]
        public async Task ProcessFailedEventsAsync_WithFailedEvents_ShouldResetAndProcess()
        {
            // Arrange
            var failedEvents = new List<OutboxEvent>
            {
                new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "correlation-1"),
                new OutboxEvent("ContactUpdated", new { Id = Guid.NewGuid() }, "correlation-2")
            };
            
            // Mark as failed
            foreach (var evt in failedEvents)
            {
                evt.MarkAsFailed("Test error");
            }
            
            _mockRepository
                .Setup(r => r.GetFailedEventsReadyForRetryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedEvents);
                
            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OutboxEvent e, CancellationToken _) => e);
                
            // Act
            await _service.ProcessFailedEventsAsync();
            
            // Assert
            _mockRepository.Verify(
                r => r.GetFailedEventsReadyForRetryAsync(50, It.IsAny<CancellationToken>()), 
                Times.Once);
            _mockRepository.Verify(
                r => r.UpdateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()), 
                Times.Exactly(4)); // 2 resets + 2 marks as processed
            _mockProducer.Verify(
                p => p.PublishAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Exactly(2));
        }
        
        [Fact]
        public async Task ProcessFailedEventsAsync_WithNoFailedEvents_ShouldDoNothing()
        {
            // Arrange
            _mockRepository
                .Setup(r => r.GetFailedEventsReadyForRetryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxEvent>());
                
            // Act
            await _service.ProcessFailedEventsAsync();
            
            // Assert
            _mockRepository.Verify(
                r => r.GetFailedEventsReadyForRetryAsync(50, It.IsAny<CancellationToken>()), 
                Times.Once);
            _mockProducer.Verify(
                p => p.PublishAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Never);
            _mockRepository.Verify(
                r => r.UpdateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }
        
        [Fact]
        public async Task CleanupProcessedEventsAsync_ShouldDeleteOldEvents()
        {
            // Arrange
            var retentionDays = 14;
            var expectedCutoff = DateTime.UtcNow.AddDays(-retentionDays);
            
            _mockRepository
                .Setup(r => r.DeleteProcessedEventsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            // Act
            await _service.CleanupProcessedEventsAsync(retentionDays);
            
            // Assert
            _mockRepository.Verify(
                r => r.DeleteProcessedEventsAsync(
                    It.Is<DateTime>(d => d.Date == expectedCutoff.Date), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact]
        public async Task GetStatisticsAsync_ShouldReturnStatistics()
        {
            // Arrange
            var pendingCount = 10;
            var failedCount = 5;
            
            _mockRepository
                .Setup(r => r.GetPendingEventCountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingCount);
                
            _mockRepository
                .Setup(r => r.GetFailedEventCountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedCount);
                
            // Act
            var stats = await _service.GetStatisticsAsync();
            
            // Assert
            stats.Should().NotBeNull();
            stats.PendingEvents.Should().Be(pendingCount);
            stats.FailedEvents.Should().Be(failedCount);
            stats.LastProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            
            _mockRepository.Verify(r => r.GetPendingEventCountAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.GetFailedEventCountAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task ProcessSingleEvent_WhenKafkaPublishFails_ShouldMarkAsFailed()
        {
            // Arrange
            var outboxEvent = new OutboxEvent("ContactCreated", new { Id = Guid.NewGuid() }, "correlation-1");
            
            _mockRepository
                .Setup(r => r.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxEvent> { outboxEvent });
                
            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OutboxEvent e, CancellationToken _) => e);
                
            _mockProducer
                .Setup(p => p.PublishAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Kafka connection failed"));
                
            // Act
            await _service.ProcessPendingEventsAsync();
            
            // Assert
            _mockRepository.Verify(
                r => r.UpdateAsync(
                    It.Is<OutboxEvent>(e => 
                        e.Status == OutboxEventStatus.Pending &&
                        e.ErrorMessage.Contains("Kafka connection failed") &&
                        e.RetryCount == 1), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Theory]
        [InlineData("ContactCreated", "contact-events")]
        [InlineData("ContactUpdated", "contact-events")]
        [InlineData("ContactDeleted", "contact-events")]
        [InlineData("ReportRequested", "report-events")]
        [InlineData("ReportCompleted", "report-events")]
        [InlineData("NotificationSent", "notification-events")]
        [InlineData("UnknownEventType", "contact-events")] // Default
        public async Task ProcessPendingEventsAsync_ShouldUseCorrectTopicForEventType(string eventType, string expectedTopic)
        {
            // Arrange
            var outboxEvent = new OutboxEvent(eventType, new { Id = Guid.NewGuid() }, "correlation-1");
            
            _mockRepository
                .Setup(r => r.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OutboxEvent> { outboxEvent });
                
            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OutboxEvent e, CancellationToken _) => e);
                
            // Act
            await _service.ProcessPendingEventsAsync();
            
            // Assert
            _mockProducer.Verify(
                p => p.PublishAsync(
                    expectedTopic, 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}
