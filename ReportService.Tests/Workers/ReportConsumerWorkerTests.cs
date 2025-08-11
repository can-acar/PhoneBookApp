using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ReportService.Domain.Interfaces;
using ReportService.Infrastructure.Services;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ReportService.ApplicationService.Worker;
using ReportService.Domain.Models.Kafka;
using Xunit;

namespace ReportService.Tests.Workers;

public class ReportConsumerWorkerTests
{
    private readonly Mock<ILogger<ReportConsumerWorker>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IKafkaConsumer> _kafkaConsumerMock;
    private readonly Mock<IReportGenerationService> _reportGenerationServiceMock;
    private readonly ReportConsumerWorker _worker;

    public ReportConsumerWorkerTests()
    {
        _loggerMock = new Mock<ILogger<ReportConsumerWorker>>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _kafkaConsumerMock = new Mock<IKafkaConsumer>();
        _reportGenerationServiceMock = new Mock<IReportGenerationService>();

        // Setup service provider chain using manual service resolution
        var scopedServiceProvider = new Mock<IServiceProvider>();
        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(scopedServiceProvider.Object);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);

        // Mock the service provider to return services manually
        var mainServiceProvider = new Mock<IServiceProvider>();
        mainServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);

        scopedServiceProvider.Setup(x => x.GetService(typeof(IKafkaConsumer)))
            .Returns(_kafkaConsumerMock.Object);

        scopedServiceProvider.Setup(x => x.GetService(typeof(IReportGenerationService)))
            .Returns(_reportGenerationServiceMock.Object);

        _worker = new ReportConsumerWorker(_loggerMock.Object, mainServiceProvider.Object,
            Options.Create(new ReportService.Infrastructure.Configuration.KafkaSettings
            {
                BootstrapServers = "localhost:9092",
                GroupId = "report-service-group",
                ClientId = "report-service",
                Topics = new ReportService.Infrastructure.Configuration.KafkaTopics
                {
                    ReportRequests = "report-requests"
                }
            }));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStartKafkaConsumer_WhenCalled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        _kafkaConsumerMock.Setup(x => x.ConsumeAsync(
                "report-requests",
                It.IsAny<Func<string, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _worker.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(200); // Wait for background task to run
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        _kafkaConsumerMock.Verify(x => x.ConsumeAsync(
            "report-requests",
            It.IsAny<Func<string, CancellationToken, Task>>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        VerifyLogContains(LogLevel.Information, "Report Consumer Worker started");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleKafkaConsumerException_AndContinue()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        var callCount = 0;
        _kafkaConsumerMock.Setup(x => x.ConsumeAsync(
                "report-requests",
                It.IsAny<Func<string, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new Exception("Kafka connection error");
                return Task.CompletedTask;
            });

        // Act
        await _worker.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(200);
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        VerifyLogContains(LogLevel.Error, "Error in report consumer worker");
        Assert.True(callCount >= 1);
    }

    [Fact]
    public async Task ProcessReportRequestAsync_ShouldGenerateReport_WhenValidMessage()
    {
        var reportId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        // Arrange
        var reportRequest = new ReportRequestMessage
        {
            ReportId = reportId,
            Location = "Istanbul",
            UserId = userId
        };

        var messageJson = JsonSerializer.Serialize(reportRequest);

        _reportGenerationServiceMock.Setup(x => x.GenerateReportAsync(
                It.IsAny<Guid>(),
                "Istanbul",
                userId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await InvokeProcessReportRequestAsync(messageJson);

        // Assert
        _reportGenerationServiceMock.Verify(x => x.GenerateReportAsync(
            It.IsAny<Guid>(),
            "Istanbul",
            userId,
            It.IsAny<CancellationToken>()), Times.Once);

        VerifyLogContains(LogLevel.Information, "Report generation completed successfully for ReportId:");
    }

    [Fact]
    public async Task ProcessReportRequestAsync_ShouldLogWarning_WhenInvalidMessageFormat()
    {
        // Arrange
        var invalidJson = "{ invalid json format }";

        // Act
        await InvokeProcessReportRequestAsync(invalidJson);

        // Assert
        VerifyLogContains(LogLevel.Warning, "Invalid report request message format");
        _reportGenerationServiceMock.Verify(x => x.GenerateReportAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReportRequestAsync_ShouldLogWarning_WhenNullMessage()
    {
        // Arrange
        var nullMessageJson = "null";

        // Act
        await InvokeProcessReportRequestAsync(nullMessageJson);

        // Assert
        VerifyLogContains(LogLevel.Warning, "Invalid report request message format");
        _reportGenerationServiceMock.Verify(x => x.GenerateReportAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReportRequestAsync_ShouldThrowException_WhenReportGenerationFails()
    {
        // Arrange
        var reportRequest = new ReportRequestMessage
        {
            ReportId = Guid.NewGuid(),
            Location = "Istanbul",
            UserId = Guid.NewGuid().ToString()
        };

        var messageJson = JsonSerializer.Serialize(reportRequest);

        _reportGenerationServiceMock.Setup(x => x.GenerateReportAsync(
                It.IsAny<Guid>(),
                "Istanbul",
                reportRequest.UserId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Report generation failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => InvokeProcessReportRequestAsync(messageJson));
        Assert.Contains("Report generation failed", exception.Message);

        VerifyLogContains(LogLevel.Error, "Error processing report request");
    }

    [Fact]
    public async Task ProcessReportRequestAsync_ShouldDeserializeCorrectly_WhenValidJson()
    {
        // Arrange
        var reportRequest = new ReportRequestMessage
        {
            ReportId = Guid.NewGuid(),
            Location = "Ankara",
            UserId = Guid.NewGuid().ToString()
        };

        var messageJson = JsonSerializer.Serialize(reportRequest);

        _reportGenerationServiceMock.Setup(x => x.GenerateReportAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await InvokeProcessReportRequestAsync(messageJson);

        // Assert
        _reportGenerationServiceMock.Verify(x => x.GenerateReportAsync(
            It.IsAny<Guid>(),
            "Ankara",
            reportRequest.UserId,
            It.IsAny<CancellationToken>()), Times.Once);

        VerifyLogContains(LogLevel.Information, "Processing report request");
        VerifyLogContains(LogLevel.Information, "Report generation completed successfully for ReportId:");
    }

    [Fact]
    public async Task StopAsync_ShouldLogInformation_WhenCalled()
    {
        // Act
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        VerifyLogContains(LogLevel.Information, "Report consumer worker is stopping");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleOperationCancelledException_Gracefully()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        _kafkaConsumerMock.Setup(x => x.ConsumeAsync(
                "report-requests",
                It.IsAny<Func<string, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        await _worker.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Brief wait
        await _worker.StopAsync(CancellationToken.None);

        // Assert - Since we're cancelling immediately, we should see either the cancellation message
        // or potentially just the stop message
        try
        {
            VerifyLogContains(LogLevel.Information, "Report Consumer Worker cancellation requested");
        }
        catch
        {
            // Alternative: may see "Report Consumer Worker stopped" instead due to timing
            VerifyLogContains(LogLevel.Information, "Report Consumer Worker stopped");
        }
    }

    // Helper method to invoke private ProcessReportRequestAsync method using reflection
    private async Task InvokeProcessReportRequestAsync(string message)
    {
        var method = typeof(ReportConsumerWorker)
            .GetMethod("ProcessReportRequestAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            await (Task)method.Invoke(_worker, new object[] { message, CancellationToken.None })!;
        }
    }

    private void VerifyLogContains(LogLevel logLevel, string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == logLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}