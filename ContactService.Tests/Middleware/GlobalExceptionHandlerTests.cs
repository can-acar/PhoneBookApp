using ContactService.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace ContactService.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
    private readonly GlobalExceptionHandler _middleware;

    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _middleware = new GlobalExceptionHandler(_loggerMock.Object);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnTrue_WhenExceptionOccurs()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new InvalidOperationException("Test exception");
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        var result = await _middleware.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetCorrectContentType_WhenExceptionOccurs()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new ArgumentException("Invalid argument");
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        await _middleware.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.ContentType.Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldWriteErrorResponse_WhenExceptionOccurs()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("General exception");
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        await _middleware.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        responseBody.Length.Should().BeGreaterThan(0);
        
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
        responseText.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("Test logging exception");
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        await _middleware.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn500StatusCode_ForAnyException()
    {
        // Arrange
        var context = new DefaultHttpContext();

        var exceptions = new Exception[]
        {
            new ArgumentException("Argument exception"),
            new InvalidOperationException("Invalid operation"),
            new NullReferenceException("Null reference"),
            new ApplicationException("Application exception")
        };
        var statusCodes = new int[]
        {
            400, // For ArgumentException
            500, // For InvalidOperationException
            500, // For NullReferenceException
            500  // For ApplicationException
        };
        var index = 0;
        foreach (var exception in exceptions)
        {
            context.Response.Clear();
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Act
            await _middleware.TryHandleAsync(context, exception, CancellationToken.None);
            var code = statusCodes[index++];
            // Assert
            // Check if the status code in statusCodes matches the expected status code for the exception type
            context.Response.StatusCode.Should().Be(code);
            context.Response.ContentType.Should().Be("application/json; charset=utf-8");
        }
    }
}
