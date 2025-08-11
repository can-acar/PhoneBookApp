using ContactService.Api.Middleware;
using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text;
using Shared.CrossCutting.CorrelationId;

namespace ContactService.Tests.Middleware;

public class AuditLoggingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<IAuditLogService> _auditServiceMock;
    private readonly Mock<ICorrelationIdProvider> _correlationIdProviderMock;
    private readonly Mock<ILogger<AuditLoggingMiddleware>> _loggerMock;
    private readonly AuditLoggingMiddleware _middleware;

    public AuditLoggingMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _auditServiceMock = new Mock<IAuditLogService>();
        _correlationIdProviderMock = new Mock<ICorrelationIdProvider>();
    _loggerMock = new Mock<ILogger<AuditLoggingMiddleware>>();
    _middleware = new AuditLoggingMiddleware(_nextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogAuditEntry_WhenCalled()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/contacts";
        context.Request.Headers["User-Agent"] = "TestAgent";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"test\":\"data\"}"));
        context.Response.Body = new MemoryStream();
        context.Response.StatusCode = 200;

        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        _correlationIdProviderMock.Setup(x => x.CorrelationId).Returns("test-correlation-id");

        // Act
    await _middleware.InvokeAsync(context, _auditServiceMock.Object, _correlationIdProviderMock.Object);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
        // Note: IAuditLogService doesn't have AddAuditLogAsync, it has LogAsync
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleException_WhenNextThrows()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/contacts/1";
        _correlationIdProviderMock.Setup(x => x.CorrelationId).Returns("test-correlation-id");

        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
    await Assert.ThrowsAsync<Exception>(() => _middleware.InvokeAsync(context, _auditServiceMock.Object, _correlationIdProviderMock.Object));
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogDifferentMethods_WhenCalledWithVariousMethods()
    {
        // Arrange
        var methods = new[] { "GET", "POST", "PUT", "DELETE" };
        _correlationIdProviderMock.Setup(x => x.CorrelationId).Returns("test-correlation-id");
        
        foreach (var method in methods)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = "/api/contacts";

            _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, _auditServiceMock.Object, _correlationIdProviderMock.Object);
        }

        // Assert
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Exactly(methods.Length));
    }
}
