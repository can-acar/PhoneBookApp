using ContactService.Infrastructure.Configuration;
using FluentAssertions;
using Xunit;

namespace ContactService.Tests.Configuration;

public class RedisSettingsTests
{
    [Fact]
    public void Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var settings = new RedisSettings();

        // Act
        settings.ConnectionString = "redis-server:6379";
        settings.Database = 1;
        settings.InstanceName = "TestService";
        settings.DefaultExpiration = TimeSpan.FromMinutes(60);
        settings.ContactCacheExpiration = TimeSpan.FromHours(2);
        settings.LocationStatsCacheExpiration = TimeSpan.FromMinutes(30);
        settings.Enabled = false;

        // Assert
        settings.ConnectionString.Should().Be("redis-server:6379");
        settings.Database.Should().Be(1);
        settings.InstanceName.Should().Be("TestService");
        settings.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(60));
        settings.ContactCacheExpiration.Should().Be(TimeSpan.FromHours(2));
        settings.LocationStatsCacheExpiration.Should().Be(TimeSpan.FromMinutes(30));
        settings.Enabled.Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new RedisSettings();

        // Assert
        settings.ConnectionString.Should().Be("localhost:6379");
        settings.Database.Should().Be(0);
        settings.InstanceName.Should().Be("ContactService");
        settings.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(30));
        settings.ContactCacheExpiration.Should().Be(TimeSpan.FromHours(1));
        settings.LocationStatsCacheExpiration.Should().Be(TimeSpan.FromMinutes(15));
        settings.Enabled.Should().BeTrue();
    }
}
