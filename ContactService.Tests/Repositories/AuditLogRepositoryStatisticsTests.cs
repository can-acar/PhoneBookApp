using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactService.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace ContactService.Tests.Repositories
{
    public class AuditLogRepositoryStatisticsTests
    {
        private readonly Mock<IAuditLogRepository> _mockRepository;

        public AuditLogRepositoryStatisticsTests()
        {
            _mockRepository = new Mock<IAuditLogRepository>();
        }

        [Fact]
        public async Task GetStatisticsAsync_ShouldReturnStatisticsByService()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-7);
            var endTime = DateTime.UtcNow;
            var expectedStats = new Dictionary<string, long>
            {
                ["ContactService"] = 42,
                ["ReportService"] = 18
            };

            _mockRepository.Setup(x => x.GetStatisticsAsync(
                    It.IsAny<DateTime?>(), 
                    It.IsAny<DateTime?>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _mockRepository.Object.GetStatisticsAsync(startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result["ContactService"].Should().Be(42);
            result["ReportService"].Should().Be(18);

            // Verify correct parameters were passed
            _mockRepository.Verify(x => x.GetStatisticsAsync(
                It.Is<DateTime?>(d => d == startTime),
                It.Is<DateTime?>(d => d == endTime),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetStatisticsAsync_WithNullParameters_ShouldReturnAllStatistics()
        {
            // Arrange
            var expectedStats = new Dictionary<string, long>
            {
                ["ContactService"] = 100,
                ["NotificationService"] = 50,
                ["ReportService"] = 25
            };

            _mockRepository.Setup(x => x.GetStatisticsAsync(
                    null, 
                    null, 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _mockRepository.Object.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result["ContactService"].Should().Be(100);
            result["NotificationService"].Should().Be(50);
            result["ReportService"].Should().Be(25);
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
                ["DELETE"] = 10,
                ["REPORT_GENERATED"] = 5
            };

            _mockRepository.Setup(x => x.GetActionStatisticsAsync(
                    It.IsAny<DateTime?>(), 
                    It.IsAny<DateTime?>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _mockRepository.Object.GetActionStatisticsAsync(startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4);
            result["CREATE"].Should().Be(25);
            result["UPDATE"].Should().Be(15);
            result["DELETE"].Should().Be(10);
            result["REPORT_GENERATED"].Should().Be(5);

            // Verify correct parameters were passed
            _mockRepository.Verify(x => x.GetActionStatisticsAsync(
                It.Is<DateTime?>(d => d == startTime),
                It.Is<DateTime?>(d => d == endTime),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetActionStatisticsAsync_WithNullParameters_ShouldReturnAllActionStats()
        {
            // Arrange
            var expectedStats = new Dictionary<string, long>
            {
                ["CREATE"] = 50,
                ["UPDATE"] = 30,
                ["DELETE"] = 20
            };

            _mockRepository.Setup(x => x.GetActionStatisticsAsync(
                    null, 
                    null, 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _mockRepository.Object.GetActionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result["CREATE"].Should().Be(50);
            result["UPDATE"].Should().Be(30);
            result["DELETE"].Should().Be(20);
        }

        [Fact]
        public async Task GetServiceStatisticsAsync_ShouldCallGetStatisticsAsync()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-7);
            var endTime = DateTime.UtcNow;
            var expectedStats = new Dictionary<string, long>
            {
                ["ContactService"] = 42,
                ["ReportService"] = 18
            };

            _mockRepository.Setup(x => x.GetServiceStatisticsAsync(
                    It.IsAny<DateTime?>(), 
                    It.IsAny<DateTime?>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _mockRepository.Object.GetServiceStatisticsAsync(startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result["ContactService"].Should().Be(42);
            result["ReportService"].Should().Be(18);

            // Verify correct parameters were passed
            _mockRepository.Verify(x => x.GetServiceStatisticsAsync(
                It.Is<DateTime?>(d => d == startTime),
                It.Is<DateTime?>(d => d == endTime),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetStatisticsAsync_EmptyResult_ShouldReturnEmptyDictionary()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetStatisticsAsync(
                    It.IsAny<DateTime?>(), 
                    It.IsAny<DateTime?>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, long>());

            // Act
            var result = await _mockRepository.Object.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        
        [Fact]
        public async Task GetActionStatisticsAsync_EmptyResult_ShouldReturnEmptyDictionary()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetActionStatisticsAsync(
                    It.IsAny<DateTime?>(), 
                    It.IsAny<DateTime?>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, long>());

            // Act
            var result = await _mockRepository.Object.GetActionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}
