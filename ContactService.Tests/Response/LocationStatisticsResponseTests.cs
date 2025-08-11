using ContactService.ApiContract.Response.Queries;
using FluentAssertions;
using Xunit;

namespace ContactService.Tests.Response;

public class LocationStatisticsResponseTests
{
    [Fact]
    public void Properties_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var response = new LocationStatisticsResponse
        {
            Location = "Istanbul",
            ContactCount = 10,
            PhoneNumberCount = 15
        };

        // Assert
        response.Location.Should().Be("Istanbul");
        response.ContactCount.Should().Be(10);
        response.PhoneNumberCount.Should().Be(15);
    }

    [Fact]
    public void DefaultValues_ShouldBeInitialized()
    {
        // Act
        var response = new LocationStatisticsResponse();

        // Assert
        response.Location.Should().Be(string.Empty);
        response.ContactCount.Should().Be(0);
        response.PhoneNumberCount.Should().Be(0);
    }

    [Fact]
    public void Location_ShouldBeSetAndRetrieved()
    {
        // Arrange
        var response = new LocationStatisticsResponse();

        // Act
        response.Location = "Ankara";

        // Assert
        response.Location.Should().Be("Ankara");
    }
}
