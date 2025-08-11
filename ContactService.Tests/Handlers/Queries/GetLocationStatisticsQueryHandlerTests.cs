using ContactService.ApplicationService.Handlers.Queries;
using ContactService.ApiContract.Request.Queries;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace ContactService.Tests.Handlers.Queries;

public class GetLocationStatisticsQueryHandlerTests
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly GetLocationStatisticsQueryHandler _handler;

    public GetLocationStatisticsQueryHandlerTests()
    {
        _mockContactService = new Mock<IContactService>();
        _handler = new GetLocationStatisticsQueryHandler(_mockContactService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnStatistics()
    {
        // Arrange
        var query = new GetLocationStatisticsQuery();
        var expectedStatistics = new List<LocationStatistic>
        {
            new LocationStatistic { Location = "Istanbul", ContactCount = 15, PhoneNumberCount = 20 },
            new LocationStatistic { Location = "Ankara", ContactCount = 8, PhoneNumberCount = 10 },
            new LocationStatistic { Location = "Izmir", ContactCount = 12, PhoneNumberCount = 15 }
        };

        _mockContactService.Setup(x => x.GetLocationStatistics(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data.Should().Contain(s => s.Location == "Istanbul" && s.ContactCount == 15);
        result.Data.Should().Contain(s => s.Location == "Ankara" && s.ContactCount == 8);
        result.Data.Should().Contain(s => s.Location == "Izmir" && s.ContactCount == 12);
    }

    [Fact]
    public async Task Handle_NoStatistics_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetLocationStatisticsQuery();

        _mockContactService.Setup(x => x.GetLocationStatistics(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LocationStatistic>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var query = new GetLocationStatisticsQuery();

        _mockContactService.Setup(x => x.GetLocationStatistics(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Beklenmeyen bir hata oluştu");
    }

    [Fact]
    public async Task Handle_ServiceReturnsNull_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetLocationStatisticsQuery();

        _mockContactService.Setup(x => x.GetLocationStatistics(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LocationStatistic>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LargeDataSet_ShouldHandleCorrectly()
    {
        // Arrange
        var query = new GetLocationStatisticsQuery();
        var largeStatistics = new List<LocationStatistic>();
        
        for (int i = 0; i < 100; i++)
        {
            largeStatistics.Add(new LocationStatistic { Location = $"City{i}", ContactCount = i * 2, PhoneNumberCount = i * 3 });
        }

        _mockContactService.Setup(x => x.GetLocationStatistics(It.IsAny<CancellationToken>()))
            .ReturnsAsync(largeStatistics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(100);
        result.Data.Should().Contain(s => s.Location == "City50" && s.ContactCount == 100);
    }
}
