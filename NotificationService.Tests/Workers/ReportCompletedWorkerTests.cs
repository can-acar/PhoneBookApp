using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NotificationService.ApplicationService.Models;
using NotificationService.ApplicationService.Worker;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using System.Text;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.Tests.Workers;

public class ReportCompletedWorkerTests : IDisposable
{
    private readonly Mock<IOptions<NotificationService.ApplicationService.Models.KafkaSettings>> _kafkaSettingsMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ITemplateService> _templateServiceMock;
    private readonly Mock<ILogger<ReportCompletedWorker>> _loggerMock;
    private readonly NotificationService.ApplicationService.Models.KafkaSettings _kafkaSettings;
    private readonly ReportCompletedWorker _worker;

    public ReportCompletedWorkerTests()
    {
        _kafkaSettingsMock = new Mock<IOptions<NotificationService.ApplicationService.Models.KafkaSettings>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _templateServiceMock = new Mock<ITemplateService>();
        _loggerMock = new Mock<ILogger<ReportCompletedWorker>>();

        _kafkaSettings = new NotificationService.ApplicationService.Models.KafkaSettings
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group",
            ReportCompletedTopic = "report-completed-test"
        };

        _kafkaSettingsMock.Setup(x => x.Value).Returns(_kafkaSettings);

        // Build a test service provider to supply scoped services for the worker
        var services = new ServiceCollection();
        services.AddScoped(_ => _notificationServiceMock.Object);
        services.AddScoped(_ => _templateServiceMock.Object);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _worker = new ReportCompletedWorker(
            _kafkaSettingsMock.Object,
            scopeFactory,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessMessage_ShouldSendEmailNotification_WhenEmailEnabled()
    {
        // Arrange
        var reportCompletedEvent = new ReportCompletedEvent
        {
            ReportId = "report-123",
            UserId = "user-456",
            Location = "Istanbul",
            UserEmail = "test@example.com",
            CompletedAt = DateTime.UtcNow,
            DownloadUrl = "https://example.com/download/report-123",
            DownloadExpiresAt = DateTime.UtcNow.AddDays(7),
            Summary = new ReportSummary
            {
                TotalPersons = 100,
                TotalPhoneNumbers = 150
            },
            NotificationPreferences = new NotificationPreferences
            {
                EnableEmail = true,
                EnableSms = false,
                Language = "tr-TR"
            }
        };

        var kafkaMessage = new KafkaMessage<ReportCompletedEvent>
        {
            Data = reportCompletedEvent,
            CorrelationId = "correlation-123"
        };

        var messageJson = JsonConvert.SerializeObject(kafkaMessage);

        _templateServiceMock.Setup(x => x.TemplateExistsAsync("ReportCompleted", "tr-TR"))
            .ReturnsAsync(true);

        _templateServiceMock.Setup(x => x.RenderNotificationTemplateAsync(
                "ReportCompleted", 
                It.IsAny<Dictionary<string, object>>(), 
                "tr-TR"))
            .ReturnsAsync(("Rapor Hazır", "Raporunuz hazır, indirme linki: https://example.com/download/report-123"));

        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(
                It.IsAny<Notification>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        _notificationServiceMock.Setup(x => x.SendNotificationAsync(
                It.IsAny<Notification>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true });

        // Act
        await InvokeProcessMessageAsync(messageJson);

        // Assert
        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            It.Is<Notification>(n => n.UserId == "user-456" && n.RecipientEmail == "test@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);

        _notificationServiceMock.Verify(x => x.SendNotificationAsync(
            It.IsAny<Notification>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_ShouldSendSmsNotification_WhenSmsEnabled()
    {
        // Arrange
        var resultGuid = Guid.NewGuid();
        var reportCompletedEvent = new ReportCompletedEvent
        {
            ReportId = "report-123",
            UserId = "user-456",
            Location = "Istanbul",
            UserPhoneNumber = "+905551234567",
            CompletedAt = DateTime.UtcNow,
            DownloadUrl = "https://example.com/download/report-123",
            DownloadExpiresAt = DateTime.UtcNow.AddDays(7),
            Summary = new ReportSummary
            {
                TotalPersons = 100,
                TotalPhoneNumbers = 150
            },
            NotificationPreferences = new NotificationPreferences
            {
                EnableEmail = false,
                EnableSms = true,
                Language = "tr-TR"
            }
        };

        var kafkaMessage = new KafkaMessage<ReportCompletedEvent>
        {
            Data = reportCompletedEvent,
            CorrelationId = "correlation-123"
        };

        var messageJson = JsonConvert.SerializeObject(kafkaMessage);

        _templateServiceMock.Setup(x => x.TemplateExistsAsync("ReportCompleted", "tr-TR"))
            .ReturnsAsync(true);

        _templateServiceMock.Setup(x => x.RenderNotificationTemplateAsync(
                "ReportCompleted", 
                It.IsAny<Dictionary<string, object>>(), 
                "tr-TR"))
            .ReturnsAsync(("Rapor Hazır", "Raporunuz hazır"));

        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(
                It.IsAny<Notification>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultGuid);

        _notificationServiceMock.Setup(x => x.SendNotificationAsync(
                It.IsAny<Notification>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true });

        // Act
        await InvokeProcessMessageAsync(messageJson);

        // Assert
        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            It.Is<Notification>(n => 
                n.UserId == "user-456" && 
                n.RecipientPhoneNumber == "+905551234567" &&
                n.PreferredProvider == ProviderType.Sms),
            It.IsAny<CancellationToken>()), Times.Once);

        _notificationServiceMock.Verify(x => x.SendNotificationAsync(
            It.IsAny<Notification>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_ShouldSendBothNotifications_WhenBothEnabled()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var reportCompletedEvent = new ReportCompletedEvent
        {
            ReportId = "report-123",
            UserId = "user-456",
            Location = "Istanbul",
            UserEmail = "test@example.com",
            UserPhoneNumber = "+905551234567",
            CompletedAt = DateTime.UtcNow,
            DownloadUrl = "https://example.com/download/report-123",
            DownloadExpiresAt = DateTime.UtcNow.AddDays(7),
            Summary = new ReportSummary
            {
                TotalPersons = 100,
                TotalPhoneNumbers = 150
            },
            NotificationPreferences = new NotificationPreferences
            {
                EnableEmail = true,
                EnableSms = true,
                Language = "tr-TR"
            }
        };

        var kafkaMessage = new KafkaMessage<ReportCompletedEvent>
        {
            Data = reportCompletedEvent,
            CorrelationId = "correlation-123"
        };

        var messageJson = JsonConvert.SerializeObject(kafkaMessage);

        _templateServiceMock.Setup(x => x.TemplateExistsAsync("ReportCompleted", "tr-TR"))
            .ReturnsAsync(true);

        _templateServiceMock.Setup(x => x.RenderNotificationTemplateAsync(
                "ReportCompleted", 
                It.IsAny<Dictionary<string, object>>(), 
                "tr-TR"))
            .ReturnsAsync(("Rapor Hazır", "Raporunuz hazır"));

        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(
                It.IsAny<Notification>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notificationId);

        _notificationServiceMock.Setup(x => x.SendNotificationAsync(
                It.IsAny<Notification>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true });

        // Act
        await InvokeProcessMessageAsync(messageJson);

        // Assert
        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            It.IsAny<Notification>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        _notificationServiceMock.Verify(x => x.SendNotificationAsync(
            It.IsAny<Notification>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessMessage_ShouldLogWarning_WhenTemplateNotFound()
    {
        // Arrange
        var reportCompletedEvent = new ReportCompletedEvent
        {
            ReportId = "report-123",
            UserId = "user-456",
            Location = "Istanbul",
            UserEmail = "test@example.com",
            NotificationPreferences = new NotificationPreferences
            {
                EnableEmail = true,
                Language = "tr-TR"
            }
        };

        var kafkaMessage = new KafkaMessage<ReportCompletedEvent>
        {
            Data = reportCompletedEvent,
            CorrelationId = "correlation-123"
        };

        var messageJson = JsonConvert.SerializeObject(kafkaMessage);

        _templateServiceMock.Setup(x => x.TemplateExistsAsync("ReportCompleted", "tr-TR"))
            .ReturnsAsync(false);

        _templateServiceMock.Setup(x => x.RenderNotificationTemplateAsync(
                "ReportCompleted", 
                It.IsAny<Dictionary<string, object>>(), 
                "tr-TR"))
            .ReturnsAsync(("Default Subject", "Default Content"));

        // Act
        await InvokeProcessMessageAsync(messageJson);

        // Assert
        VerifyLogContains(LogLevel.Warning, "Template 'ReportCompleted' not found for language tr-TR");
    }

    [Fact]
    public async Task ProcessMessage_ShouldLogError_WhenInvalidMessageFormat()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        await InvokeProcessMessageAsync(invalidJson);

        // Assert
        VerifyLogContains(LogLevel.Error, "Error deserializing Kafka message");
    }

    [Fact]
    public async Task ProcessMessage_ShouldLogWarning_WhenMessageDataIsNull()
    {
        // Arrange
        var kafkaMessage = new KafkaMessage<ReportCompletedEvent>
        {
            Data = null,
            CorrelationId = "correlation-123"
        };

        var messageJson = JsonConvert.SerializeObject(kafkaMessage);

        // Act
        await InvokeProcessMessageAsync(messageJson);

        // Assert
        VerifyLogContains(LogLevel.Warning, "Invalid message format or empty data");
    }

    [Fact]
    public async Task ProcessMessage_ShouldLogError_WhenNotificationServiceThrows()
    {
        // Arrange
        var reportCompletedEvent = new ReportCompletedEvent
        {
            ReportId = "report-123",
            UserId = "user-456",
            Location = "Istanbul",
            UserEmail = "test@example.com",
            NotificationPreferences = new NotificationPreferences
            {
                EnableEmail = true,
                Language = "tr-TR"
            }
        };

        var kafkaMessage = new KafkaMessage<ReportCompletedEvent>
        {
            Data = reportCompletedEvent,
            CorrelationId = "correlation-123"
        };

        var messageJson = JsonConvert.SerializeObject(kafkaMessage);

        _templateServiceMock.Setup(x => x.TemplateExistsAsync("ReportCompleted", "tr-TR"))
            .ReturnsAsync(true);

        _templateServiceMock.Setup(x => x.RenderNotificationTemplateAsync(
                "ReportCompleted", 
                It.IsAny<Dictionary<string, object>>(), 
                "tr-TR"))
            .ReturnsAsync(("Subject", "Content"));

        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(
                It.IsAny<Notification>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Notification service error"));

        // Act
        await InvokeProcessMessageAsync(messageJson);

        // Assert
        VerifyLogContains(LogLevel.Error, "Error sending notification for completed report report-123");
    }

    // Helper method to invoke the private ProcessMessageAsync method using reflection
    private async Task InvokeProcessMessageAsync(string message)
    {
        var method = typeof(ReportCompletedWorker)
            .GetMethod("ProcessMessageAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
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

    public void Dispose()
    {
        _worker?.Dispose();
    }
}

// Helper classes for testing
public class KafkaMessage<T>
{
    public T? Data { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class ReportCompletedEvent
{
    public string ReportId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? UserPhoneNumber { get; set; }
    public DateTime CompletedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime DownloadExpiresAt { get; set; }
    public ReportSummary? Summary { get; set; }
    public NotificationPreferences? NotificationPreferences { get; set; }
}

public class ReportSummary
{
    public int TotalPersons { get; set; }
    public int TotalPhoneNumbers { get; set; }
}

public class NotificationPreferences
{
    public bool EnableEmail { get; set; }
    public bool EnableSms { get; set; }
    public string Language { get; set; } = "tr-TR";
}

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string ReportCompletedTopic { get; set; } = string.Empty;
}