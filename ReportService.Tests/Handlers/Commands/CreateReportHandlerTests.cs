// Define delegate for TryGetValue callback
using System;
using System.Threading;
using System.Threading.Tasks;
using ReportService.ApplicationService.Handlers.Commands;
using ReportService.ApiContract.Request.Commands;
using ReportService.ApiContract.Response.Commands;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Interfaces;
using ReportService.Domain.Events;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using Shared.CrossCutting.Models;

namespace ReportService.Tests.Handlers.Commands;

delegate void TryGetValueCallback<TKey, TValue>(TKey key, out TValue value);

[Trait("Category", "Unit")]
public class CreateReportHandlerTests
{
    private readonly Mock<IReportRepository> _mockReportRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<CreateReportHandler>> _mockLogger;
    private readonly CreateReportHandler _handler;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockHttpRequest;
    private readonly Mock<IHeaderDictionary> _mockHeaderDictionary;

    public CreateReportHandlerTests()
    {
        _mockReportRepository = new Mock<IReportRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<CreateReportHandler>>();
        
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpRequest = new Mock<HttpRequest>();
        _mockHeaderDictionary = new Mock<IHeaderDictionary>();
        
        _mockHttpContext.Setup(c => c.Request).Returns(_mockHttpRequest.Object);
        _mockHttpRequest.Setup(r => r.Headers).Returns(_mockHeaderDictionary.Object);
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(_mockHttpContext.Object);
        
        _handler = new CreateReportHandler(
            _mockReportRepository.Object, 
            _mockEventPublisher.Object,
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateReportAndReturnId()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new CreateReportCommand
        {
            Location = "İstanbul",
            RequestedBy = "test-user"
        };
        
        Report capturedReport = null!;
        
        _mockReportRepository
            .Setup(r => r.CreateAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()))
            .Callback<Report, CancellationToken>((report, _) => capturedReport = report)
            .ReturnsAsync((Report report, CancellationToken _) => report);
        
        _mockEventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReportRequestedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ApiResponse<CreateReportResponse>>();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ReportId.Should().NotBeEmpty();
        result.Data.Status.Should().Be(ReportStatus.Preparing.ToString());
        result.Data.Message.Should().NotBeNullOrEmpty();
        
        capturedReport.Should().NotBeNull();
        capturedReport.Location.Should().Be("İstanbul");
        capturedReport.RequestedBy.Should().Be("test-user");
        capturedReport.Status.Should().Be(ReportStatus.Preparing);
        
        _mockReportRepository.Verify(r => r.CreateAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(p => p.PublishAsync(
            It.IsAny<string>(), It.IsAny<ReportRequestedEvent>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData("", "test-user")]
    [InlineData("  ", "test-user")]
    public async Task Handle_InvalidLocation_ShouldCreateReportWithEmptyLocation(string location, string requestedBy)
    {
        // Arrange
        var request = new CreateReportCommand
        {
            Location = location,
            RequestedBy = requestedBy
        };

        _mockReportRepository
            .Setup(r => r.CreateAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Report report, CancellationToken _) => report);
        
        _mockEventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReportRequestedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithCorrelationId_ShouldIncludeItInEvent()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new CreateReportCommand
        {
            Location = "İstanbul",
            RequestedBy = "test-user"
        };
        
        var correlationId = "test-correlation-id";
        Microsoft.Extensions.Primitives.StringValues headerValues = new Microsoft.Extensions.Primitives.StringValues(correlationId);
        
        _mockHeaderDictionary
            .Setup(h => h.TryGetValue("X-Correlation-ID", out It.Ref<Microsoft.Extensions.Primitives.StringValues>.IsAny))
            .Callback(new TryGetValueCallback<string, Microsoft.Extensions.Primitives.StringValues>(
                (string key, out Microsoft.Extensions.Primitives.StringValues values) => 
                {
                    values = headerValues;
                    return;
                }))
            .Returns(true);
        
        ReportRequestedEvent? capturedEvent = null;
        
        _mockReportRepository
            .Setup(r => r.CreateAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Report report, CancellationToken _) => report);
        
        _mockEventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReportRequestedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<string, ReportRequestedEvent, CancellationToken>((_, evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.CorrelationId.Should().Be(correlationId);
    }
}
