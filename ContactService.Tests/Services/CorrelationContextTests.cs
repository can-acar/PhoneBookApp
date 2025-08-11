using System.Collections.Generic;
using ContactService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace ContactService.Tests.Services;

public class CorrelationContextTests
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly CorrelationContext _correlationContext;
    private readonly DefaultHttpContext _httpContext;

    public CorrelationContextTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);
        
        _correlationContext = new CorrelationContext(_mockHttpContextAccessor.Object);
    }

    [Fact]
    public void CorrelationId_WhenInRequestHeader_ShouldReturnValueFromRequestHeader()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id";
        _httpContext.Request.Headers[CorrelationIdHeaderName] = expectedCorrelationId;

        // Act
        var result = _correlationContext.CorrelationId;

        // Assert
        result.Should().Be(expectedCorrelationId);
    }

    [Fact]
    public void CorrelationId_WhenInResponseHeaderOnly_ShouldReturnValueFromResponseHeader()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id";
        _httpContext.Response.Headers[CorrelationIdHeaderName] = expectedCorrelationId;

        // Act
        var result = _correlationContext.CorrelationId;

        // Assert
        result.Should().Be(expectedCorrelationId);
    }

    [Fact]
    public void CorrelationId_WhenInBothHeaders_ShouldPreferRequestHeader()
    {
        // Arrange
        var requestCorrelationId = "request-correlation-id";
        var responseCorrelationId = "response-correlation-id";
        
        _httpContext.Request.Headers[CorrelationIdHeaderName] = requestCorrelationId;
        _httpContext.Response.Headers[CorrelationIdHeaderName] = responseCorrelationId;

        // Act
        var result = _correlationContext.CorrelationId;

        // Assert
        result.Should().Be(requestCorrelationId);
    }

    [Fact]
    public void CorrelationId_WhenNoHeaderExists_ShouldReturnNull()
    {
        // Act
        var result = _correlationContext.CorrelationId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CorrelationId_WhenHttpContextIsNull_ShouldReturnNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

        // Act
        var result = _correlationContext.CorrelationId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SetCorrelationId_ShouldAddToResponseHeader()
    {
        // Arrange
        var correlationId = "new-correlation-id";

        // Act
        _correlationContext.SetCorrelationId(correlationId);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey(CorrelationIdHeaderName);
        _httpContext.Response.Headers[CorrelationIdHeaderName].ToString().Should().Be(correlationId);
    }

    [Fact]
    public void SetCorrelationId_WhenHttpContextIsNull_ShouldNotThrowException()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

        // Act & Assert
        var action = () => _correlationContext.SetCorrelationId("test-id");
        action.Should().NotThrow();
    }
}
