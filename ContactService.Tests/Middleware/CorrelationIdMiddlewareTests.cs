using ContactService.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace ContactService.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<CorrelationIdMiddleware>> _loggerMock;
    private readonly CorrelationIdMiddleware _middleware;

    public CorrelationIdMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<CorrelationIdMiddleware>>();
        _middleware = new CorrelationIdMiddleware(_nextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddCorrelationId_WhenNotPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Correlation-ID");
        context.Response.Headers["X-Correlation-ID"].Should().NotBeEmpty();
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldUseExistingCorrelationId_WhenPresent()
    {
        // Arrange
        var existingCorrelationId = "existing-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = existingCorrelationId;
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Correlation-ID"].ToString().Should().Be(existingCorrelationId);
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_Always()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPropagateException_WhenNextThrows()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedException = new InvalidOperationException("Test exception");
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _middleware.InvokeAsync(context));
        thrownException.Should().Be(expectedException);
    }
}
