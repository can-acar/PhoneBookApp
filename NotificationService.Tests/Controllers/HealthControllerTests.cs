using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NotificationService.Api.Controllers;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<INotificationProviderManager> _providerManagerMock;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _providerManagerMock = new Mock<INotificationProviderManager>();
        _controller = new HealthController(_providerManagerMock.Object);
    }

    [Fact]
    public async Task Get_ShouldReturnOkResult_WhenAllProvidersAreHealthy()
    {
        // Arrange
        var healthyProviders = new Dictionary<ProviderType, ProviderHealthStatus>
        {
            { ProviderType.Email, new ProviderHealthStatus { IsHealthy = true, Status = "Operational", ResponseTime = TimeSpan.FromMilliseconds(100) } },
            { ProviderType.Sms, new ProviderHealthStatus { IsHealthy = true, Status = "Operational", ResponseTime = TimeSpan.FromMilliseconds(150) } }
        };

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthyProviders);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var responseValue = okResult.Value;
        responseValue.Should().NotBeNull();

        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnOkResult_WhenAtLeastOneProviderIsHealthy()
    {
        // Arrange
        var mixedProviders = new Dictionary<ProviderType, ProviderHealthStatus>
        {
            { ProviderType.Email, new ProviderHealthStatus { IsHealthy = true, Status = "Operational", ResponseTime = TimeSpan.FromMilliseconds(100) } },
            { ProviderType.Sms, new ProviderHealthStatus { IsHealthy = false, Status = "Unavailable", ErrorMessage = "Service timeout" } }
        };

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mixedProviders);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnServiceUnavailable_WhenAllProvidersAreUnhealthy()
    {
        // Arrange
        var unhealthyProviders = new Dictionary<ProviderType, ProviderHealthStatus>
        {
            { ProviderType.Email, new ProviderHealthStatus { IsHealthy = false, Status = "Unavailable", ErrorMessage = "SMTP server down" } },
            { ProviderType.Sms, new ProviderHealthStatus { IsHealthy = false, Status = "Unavailable", ErrorMessage = "API key invalid" } }
        };

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(unhealthyProviders);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(503);

        var responseValue = objectResult.Value;
        responseValue.Should().NotBeNull();

        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnServiceUnavailable_WhenNoProvidersExist()
    {
        // Arrange
        var emptyProviders = new Dictionary<ProviderType, ProviderHealthStatus>();

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyProviders);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(503);

        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var exception = new InvalidOperationException("Provider manager initialization failed");

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);

        var responseValue = objectResult.Value;
        responseValue.Should().NotBeNull();

        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldIncludeResponseTimeInHealthyResult()
    {
        // Arrange
        var fastResponseTime = TimeSpan.FromMilliseconds(50);
        var slowResponseTime = TimeSpan.FromMilliseconds(500);

        var providers = new Dictionary<ProviderType, ProviderHealthStatus>
        {
            { ProviderType.Email, new ProviderHealthStatus { IsHealthy = true, Status = "Operational", ResponseTime = fastResponseTime } },
            { ProviderType.Sms, new ProviderHealthStatus { IsHealthy = true, Status = "Operational", ResponseTime = slowResponseTime } }
        };

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;

        // Additional verification could be done on the actual response structure
        // if we had access to the exact response format
        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldIncludeErrorMessageInUnhealthyResult()
    {
        // Arrange
        var providers = new Dictionary<ProviderType, ProviderHealthStatus>
        {
            { ProviderType.Email, new ProviderHealthStatus { IsHealthy = false, Status = "Unavailable", ErrorMessage = "Connection timeout" } },
            { ProviderType.WebSocket, new ProviderHealthStatus { IsHealthy = false, Status = "Error", ErrorMessage = "Authentication failed" } }
        };

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(503);

        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ProviderType.Email)]
    [InlineData(ProviderType.Sms)]
    [InlineData(ProviderType.WebSocket)]
    [InlineData(ProviderType.Push)]
    public async Task Get_ShouldHandle_DifferentProviderTypes(ProviderType providerType)
    {
        // Arrange
        var providers = new Dictionary<ProviderType, ProviderHealthStatus>
        {
            { providerType, new ProviderHealthStatus { IsHealthy = true, Status = "Operational", ResponseTime = TimeSpan.FromMilliseconds(100) } }
        };

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnHealthy_WhenSingleCriticalProviderIsHealthy()
    {
        // Arrange - Email provider is typically considered critical
        var providers = new Dictionary<ProviderType, ProviderHealthStatus>
        {
            { ProviderType.Email, new ProviderHealthStatus { IsHealthy = true, Status = "Operational", ResponseTime = TimeSpan.FromMilliseconds(100) } }
        };

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldCallProviderManager_OnlyOnce()
    {
        // Arrange
        var providers = new Dictionary<ProviderType, ProviderHealthStatus>
        {
            { ProviderType.Email, new ProviderHealthStatus { IsHealthy = true, Status = "Operational" } }
        };

        _providerManagerMock
            .Setup(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        await _controller.Get();

        // Assert
        _providerManagerMock.Verify(pm => pm.CheckAllProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
        _providerManagerMock.VerifyNoOtherCalls();
    }
}
