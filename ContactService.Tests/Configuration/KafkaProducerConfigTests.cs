using ContactService.Infrastructure.Configuration;
using FluentAssertions;
using Xunit;

namespace ContactService.Tests.Configuration;

public class KafkaSettingsTests
{
    [Fact]
    public void KafkaSettings_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var settings = new KafkaSettings();

        // Act
        settings.BootstrapServers = "localhost:9092";
        settings.GroupId = "contact-service";
        settings.ClientId = "contact-service-client";

        // Assert
        settings.BootstrapServers.Should().Be("localhost:9092");
        settings.GroupId.Should().Be("contact-service");
        settings.ClientId.Should().Be("contact-service-client");
        settings.Topics.Should().NotBeNull();
        settings.ProducerConfig.Should().NotBeNull();
    }

    [Fact]
    public void KafkaTopics_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var topics = new KafkaTopics();

        // Act
        topics.ContactEvents = "contact-events";
        topics.ReportEvents = "report-events";
        topics.NotificationEvents = "notification-events";

        // Assert
        topics.ContactEvents.Should().Be("contact-events");
        topics.ReportEvents.Should().Be("report-events");
        topics.NotificationEvents.Should().Be("notification-events");
    }

    [Fact]
    public void KafkaProducerConfig_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var config = new KafkaProducerConfig();

        // Act
        config.Acks = "1";
        config.MessageTimeoutMs = 60000;
        config.RequestTimeoutMs = 60000;
        config.EnableIdempotence = false;
        config.Retries = 5;
        config.RetryBackoffMs = 200;
        config.BatchSize = 32768;
        config.LingerMs = 20;
        config.CompressionType = "gzip";

        // Assert
        config.Acks.Should().Be("1");
        config.MessageTimeoutMs.Should().Be(60000);
        config.RequestTimeoutMs.Should().Be(60000);
        config.EnableIdempotence.Should().BeFalse();
        config.Retries.Should().Be(5);
        config.RetryBackoffMs.Should().Be(200);
        config.BatchSize.Should().Be(32768);
        config.LingerMs.Should().Be(20);
        config.CompressionType.Should().Be("gzip");
    }

    [Fact]
    public void KafkaProducerConfig_DefaultValues_ShouldBeSet()
    {
        // Act
        var config = new KafkaProducerConfig();

        // Assert
        config.Acks.Should().Be("all");
        config.MessageTimeoutMs.Should().Be(30000);
        config.RequestTimeoutMs.Should().Be(30000);
        config.EnableIdempotence.Should().BeTrue();
        config.Retries.Should().Be(3);
        config.RetryBackoffMs.Should().Be(100);
        config.BatchSize.Should().Be(16384);
        config.LingerMs.Should().Be(10);
        config.CompressionType.Should().Be("snappy");
    }
}
