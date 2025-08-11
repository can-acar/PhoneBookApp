using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ReportService.Api.Controllers;
using ReportService.ApiContract.Contracts;
using ReportService.ApiContract.Request.Commands;
using ReportService.ApiContract.Request.Queries;
using ReportService.ApiContract.Response.Commands;
using Shared.CrossCutting.Models;

namespace ReportService.Tests.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new ReportsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task CreateReport_ShouldReturnCreatedResult_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateReportCommand
        {
            Location = "Istanbul",
            RequestedBy = "TestUser"
        };

        var responseData = new CreateReportResponse
        {
            ReportId = Guid.NewGuid(),
            Status = "Preparing",
            Message = "Report request received and queued for processing"
        };

        var apiResponse = ApiResponse.Result(true, responseData, 200, "Success");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateReportCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await _controller.CreateReport(command);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(apiResponse);
        createdResult.ActionName.Should().Be(nameof(ReportsController.GetReportById));
        createdResult.RouteValues!["id"].Should().Be(responseData.ReportId);

        _mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllReports_ShouldReturnOkResult_WithReportsList()
    {
        // Arrange
        var reportsData = new List<ReportDto>
        {
            new ReportDto
            {
                Id = Guid.NewGuid(),
                Status = "Completed",
                RequestedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow,
                LocationStatistics = new List<LocationStatisticDto>()
            },
            new ReportDto
            {
                Id = Guid.NewGuid(),
                Status = "Preparing",
                RequestedAt = DateTime.UtcNow,
                CompletedAt = null,
                LocationStatistics = new List<LocationStatisticDto>()
            }
        };

        var apiResponse = ApiResponse.Result(true, reportsData, 200, "Success");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllReportsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await _controller.GetAllReports();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(apiResponse);

        _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllReportsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReportById_ShouldReturnOkResult_WhenReportExists()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reportData = new ReportDto
        {
            Id = reportId,
            Status = "Completed",
            RequestedAt = DateTime.UtcNow.AddDays(-1),
            CompletedAt = DateTime.UtcNow,
            LocationStatistics = new List<LocationStatisticDto>()
        };

        var apiResponse = ApiResponse.Result(true, reportData, 200, "Success");

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetReportByIdQuery>(q => q.Id == reportId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await _controller.GetReportById(reportId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(apiResponse);

        _mediatorMock.Verify(m => m.Send(It.Is<GetReportByIdQuery>(q => q.Id == reportId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReportById_ShouldReturnNotFound_WhenReportDoesNotExist()
    {
        // Arrange
        var reportId = Guid.NewGuid();

        var apiResponse = ApiResponse.Result<ReportDto>(false, null, 404, "Report not found");

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetReportByIdQuery>(q => q.Id == reportId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await _controller.GetReportById(reportId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        var notFoundResult = (NotFoundResult)result;
        notFoundResult.StatusCode.Should().Be(404);

        _mediatorMock.Verify(m => m.Send(It.Is<GetReportByIdQuery>(q => q.Id == reportId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReport_ShouldCallMediator_WithCorrectParameters()
    {
        // Arrange
        var command = new CreateReportCommand
        {
            Location = "Ankara",
            RequestedBy = "Admin"
        };

        var responseData = new CreateReportResponse
        {
            ReportId = Guid.NewGuid(),
            Status = "Preparing"
        };

        var apiResponse = ApiResponse.Result(true, responseData, 200, "Success");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateReportCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _controller.CreateReport(command);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateReportCommand>(cmd => 
                cmd.Location == "Ankara" && 
                cmd.RequestedBy == "Admin"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetAllReports_ShouldReturn_EmptyList_WhenNoReportsExist()
    {
        // Arrange
        var emptyReports = new List<ReportDto>();
        var apiResponse = ApiResponse.Result(true, emptyReports, 200, "Success");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllReportsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await _controller.GetAllReports();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        
        var returnedApiResponse = okResult.Value as ApiResponse<List<ReportDto>>;
        returnedApiResponse.Should().NotBeNull();
        returnedApiResponse!.Success.Should().BeTrue();
        returnedApiResponse.Data.Should().NotBeNull();
        returnedApiResponse.Data.Should().BeEmpty();

        _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllReportsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TestLocation")]
    [InlineData("İstanbul")]
    public async Task CreateReport_ShouldAccept_DifferentLocationValues(string location)
    {
        // Arrange
        var command = new CreateReportCommand
        {
            Location = location,
            RequestedBy = "TestUser"
        };

        var responseData = new CreateReportResponse
        {
            ReportId = Guid.NewGuid(),
            Status = "Preparing"
        };

        var apiResponse = ApiResponse.Result(true, responseData, 200, "Success");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateReportCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await _controller.CreateReport(command);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);

        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateReportCommand>(cmd => cmd.Location == location), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetReportById_ShouldCreateCorrectQuery_WithProvidedId()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reportData = new ReportDto { Id = reportId };
        var apiResponse = ApiResponse.Result(true, reportData, 200, "Success");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetReportByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _controller.GetReportById(reportId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetReportByIdQuery>(q => q.Id == reportId), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
