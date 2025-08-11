using ContactService.ApplicationService.Handlers.Queries;
using ContactService.ApiContract.Request.Queries;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Entities;
using ContactService.ApiContract.Contracts;
using FluentAssertions;
using Moq;
using Xunit;
using Shared.CrossCutting.Models;

namespace ContactService.Tests.Handlers.Queries;

public class GetContactsByLocationHandlerTestsOld
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly GetContactsByLocationHandler _handler;

    public GetContactsByLocationHandlerTestsOld()
    {
        _mockContactService = new Mock<IContactService>();
        _handler = new GetContactsByLocationHandler(_mockContactService.Object);
    }

    [Fact]
    public async Task Handle_ValidLocation_ShouldReturnContacts()
    {
        // Arrange
        var location = "Istanbul";
        var query = new GetContactsByLocationQuery { Location = location, Page = 1, PageSize = 10 };
        var contacts = new List<Contact>
        {
            new Contact("John", "Doe", "Tech Corp"),
            new Contact("Jane", "Smith", "Another Corp")
        };

        _mockContactService.Setup(x => x.GetContactsFilterByLocation(1, 10, location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pagination<Contact>(contacts, 1, 10, 2));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var pageResponse = result.Data;
        pageResponse.Should().NotBeNull();
        pageResponse.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyLocation_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetContactsByLocationQuery { Location = "", Page = 1, PageSize = 10 };

        _mockContactService.Setup(x => x.GetContactsFilterByLocation(1, 10, "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pagination<Contact>(new List<Contact>(), 1, 10, 0));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        var pageResponse = result.Data;
        pageResponse.Should().NotBeNull();
        pageResponse!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var query = new GetContactsByLocationQuery { Location = "Istanbul", Page = 1, PageSize = 10 };

        _mockContactService.Setup(x => x.GetContactsFilterByLocation(1, 10, "Istanbul", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Beklenmeyen bir hata oluştu");
    }

    [Fact]
    public async Task Handle_NonExistentLocation_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetContactsByLocationQuery { Location = "NonExistentCity", Page = 1, PageSize = 10 };

        _mockContactService.Setup(x => x.GetContactsFilterByLocation(1, 10, "NonExistentCity", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pagination<Contact>(new List<Contact>(), 1, 10, 0));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        var pageResponse = result.Data;
        pageResponse.Should().NotBeNull();
        pageResponse!.Data.Should().BeEmpty();
    }
}
