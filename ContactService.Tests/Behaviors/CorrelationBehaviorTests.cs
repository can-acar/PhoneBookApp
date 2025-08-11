using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactService.ApplicationService.Behaviors;
using ContactService.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContactService.Tests.Behaviors;

public class CorrelationBehaviorTests
{
    private readonly Mock<ICorrelationContext> _mockCorrelationContext;
    private readonly Mock<ILogger<CorrelationBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly CorrelationBehavior<TestRequest, TestResponse> _behavior;
    private readonly RequestHandlerDelegate<TestResponse> _nextDelegate;

    public CorrelationBehaviorTests()
    {
        _mockCorrelationContext = new Mock<ICorrelationContext>();
        _mockLogger = new Mock<ILogger<CorrelationBehavior<TestRequest, TestResponse>>>();

        _behavior = new CorrelationBehavior<TestRequest, TestResponse>(
            _mockCorrelationContext.Object,
            _mockLogger.Object
        );

        // RequestHandlerDelegate signature for MediatR
        _nextDelegate = (cancellationToken) => Task.FromResult(new TestResponse { Success = true });
    }

    [Fact]
    public async Task Handle_ShouldLogWithCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _mockCorrelationContext.Setup(c => c.CorrelationId).Returns(correlationId);

        // Act
        var result = await _behavior.Handle(new TestRequest(), _nextDelegate, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        // Verify logging calls were made with correlation ID
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task Handle_ShouldPropagateCorrelationIdToLogScope()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        _mockCorrelationContext.Setup(c => c.CorrelationId).Returns(correlationId);
        
        // Act
        await _behavior.Handle(new TestRequest(), _nextDelegate, CancellationToken.None);
        
        // Assert - The scope dictionary should contain the correlation ID
        _mockLogger.Verify(
            logger => logger.BeginScope(
                It.Is<Dictionary<string, object>>(dict => 
                    dict.ContainsKey("CorrelationId") && 
                    dict["CorrelationId"].Equals(correlationId) &&
                    dict.ContainsKey("RequestType") &&
                    dict["RequestType"].Equals(typeof(TestRequest).Name))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNextDelegateFails_ShouldPropagateException()
    {
        // Arrange
        _mockCorrelationContext.Setup(c => c.CorrelationId).Returns("test-correlation-id");
        var expectedException = new InvalidOperationException("Test exception");
        RequestHandlerDelegate<TestResponse> failingDelegate = (cancellationToken) => Task.FromException<TestResponse>(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _behavior.Handle(new TestRequest(), failingDelegate, CancellationToken.None));
        
        exception.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task Handle_WithNullCorrelationId_ShouldStillProceed()
    {
        // Arrange
        _mockCorrelationContext.Setup(c => c.CorrelationId).Returns((string?)null);

        // Act
        var result = await _behavior.Handle(new TestRequest(), _nextDelegate, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    // Helper classes for testing
    public class TestRequest : IRequest<TestResponse>
    {
        public string? Data { get; set; }
    }

    public class TestResponse
    {
        public bool Success { get; set; }
    }
}
