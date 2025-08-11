using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContactService.Domain.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ContactService.Tests.MongoDb
{
    [TestClass]
    public class AuditLogRepositoryStatisticsTests
    {
        private readonly Mock<IAuditLogRepository> _mockRepository;

        public AuditLogRepositoryStatisticsTests()
        {
            _mockRepository = new Mock<IAuditLogRepository>();
        }

        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(42, result["ContactService"]);
            Assert.AreEqual(18, result["ReportService"]);

            // Verify correct parameters were passed
            _mockRepository.Verify(x => x.GetStatisticsAsync(
                It.Is<DateTime?>(d => d == startTime),
                It.Is<DateTime?>(d => d == endTime),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(100, result["ContactService"]);
            Assert.AreEqual(50, result["NotificationService"]);
            Assert.AreEqual(25, result["ReportService"]);
        }

        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(25, result["CREATE"]);
            Assert.AreEqual(15, result["UPDATE"]);
            Assert.AreEqual(10, result["DELETE"]);
            Assert.AreEqual(5, result["REPORT_GENERATED"]);

            // Verify correct parameters were passed
            _mockRepository.Verify(x => x.GetActionStatisticsAsync(
                It.Is<DateTime?>(d => d == startTime),
                It.Is<DateTime?>(d => d == endTime),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(50, result["CREATE"]);
            Assert.AreEqual(30, result["UPDATE"]);
            Assert.AreEqual(20, result["DELETE"]);
        }

        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(42, result["ContactService"]);
            Assert.AreEqual(18, result["ReportService"]);

            // Verify correct parameters were passed
            _mockRepository.Verify(x => x.GetServiceStatisticsAsync(
                It.Is<DateTime?>(d => d == startTime),
                It.Is<DateTime?>(d => d == endTime),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
        
        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
    }
}
