using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
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
public class GetReportByIdHandlerTests
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly GetReportByIdHandler _handler;

    public GetReportByIdHandlerTests()
    {
        _mockReportService = new Mock<IReportService>();
        _handler = new GetReportByIdHandler(_mockReportService.Object);
    }

    [Fact]
    public async Task Handle_ExistingReport_ShouldReturnReport()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new GetReportByIdQuery { Id = reportId };

        var report = new Report("İstanbul", "test-user");
        report.MarkAsInProgress();
        report.MarkAsCompleted(150, 180);
        report.AddLocationStatistic("İstanbul", 150, 180);

        _mockReportService
            .Setup(s => s.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ApiResponse<ReportDto>>();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(report.Id);
        result.Data.Status.Should().Be(ReportStatus.Completed.ToString());
        result.Data.RequestedAt.Should().BeCloseTo(report.RequestedAt, TimeSpan.FromSeconds(5));
        result.Data.LocationStatistics.Should().NotBeNull();
        result.Data.LocationStatistics.Should().HaveCount(1);
        result.Data.LocationStatistics[0].Location.Should().Be("İstanbul");
        result.Data.LocationStatistics[0].PersonCount.Should().Be(150);
        result.Data.LocationStatistics[0].PhoneNumberCount.Should().Be(180);
        
        _mockReportService.Verify(s => s.GetByIdAsync(
            reportId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingReport_ShouldReturnNull()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new GetReportByIdQuery { Id = reportId };

        _mockReportService
            .Setup(s => s.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Report?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        
        _mockReportService.Verify(s => s.GetByIdAsync(
            reportId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceException_ShouldPropagateException()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new GetReportByIdQuery { Id = reportId };

        _mockReportService
            .Setup(s => s.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
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
