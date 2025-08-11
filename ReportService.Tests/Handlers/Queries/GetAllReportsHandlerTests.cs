using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ReportService.ApplicationService.Handlers.Queries;
using ReportService.ApiContract.Request.Queries;
using ReportService.ApiContract.Contracts;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;
using Shared.CrossCutting.Models;

namespace ReportService.Tests.Handlers.Queries;

[Trait("Category", "Unit")]
public class GetAllReportsHandlerTests
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly GetAllReportsHandler _handler;

    public GetAllReportsHandlerTests()
    {
        _mockReportService = new Mock<IReportService>();
        _handler = new GetAllReportsHandler(_mockReportService.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllReports()
    {
        // Arrange
        var request = new GetAllReportsQuery();
        var reports = new List<Report>
        {
            new("İstanbul", "user-1"),
            new("Ankara", "user-2"),
            new("İzmir", "user-3")
        };

        // Set up each report with appropriate properties
        reports[0].MarkAsInProgress();
        reports[0].MarkAsCompleted(150, 180);
        
        reports[1].MarkAsInProgress();
        
        // The third report is left in Preparing status

        _mockReportService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ApiResponse<List<ReportDto>>>();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(3);
        result.Data.Select(r => r.Status).Should().BeEquivalentTo(
            new[] { ReportStatus.Completed.ToString(), ReportStatus.InProgress.ToString(), ReportStatus.Preparing.ToString() }
        );
        
        _mockReportService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoReports_ShouldReturnEmptyList()
    {
        // Arrange
        var request = new GetAllReportsQuery();
        var reports = new List<Report>();

        _mockReportService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().BeEmpty();
        
        _mockReportService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceException_ShouldPropagateException()
    {
        // Arrange
        var request = new GetAllReportsQuery();

        _mockReportService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Beklenmeyen hata"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Contain("error");
    }
}
