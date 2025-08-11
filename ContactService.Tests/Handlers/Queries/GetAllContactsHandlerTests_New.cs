using ContactService.ApplicationService.Handlers.Queries;
using ContactService.ApiContract.Request.Queries;
using ContactService.Domain;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;
using Shared.CrossCutting.Models;

namespace ContactService.Tests.Handlers.Queries;

public class GetAllContactsHandlerTests
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly GetAllContactsHandler _handler;

    public GetAllContactsHandlerTests()
    {
        _mockContactService = new Mock<IContactService>();
        _handler = new GetAllContactsHandler(_mockContactService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnContactsList()
    {
        // Arrange
        var request = new GetAllContactsQuery
        {
            Page = 1,
            PageSize = 10
        };

        var contacts = new List<Contact>
        {
            new("John", "Doe", "Tech Corp"),
            new("Jane", "Smith", "Example Corp"),
            new("Ali", "Veli", "Istanbul Tech")
        };

        var paginationResult = new Pagination<Contact>(contacts, 1, 10, contacts.Count);

        _mockContactService.Setup(x => x.GetAllContactsAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Data.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(3);
        result.Data.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldReturnFilteredContacts()
    {
        // Arrange
        var request = new GetAllContactsQuery
        {
            Page = 1,
            PageSize = 10,
            SearchTerm = "John"
        };

        var contacts = new List<Contact>
        {
            new("John", "Doe", "Tech Corp")
        };

        var paginationResult = new Pagination<Contact>(contacts, 1, 10, 1);

        _mockContactService.Setup(x => x.GetAllContactsAsync(1, 10, "John", It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Data.Should().HaveCount(1);
        result.Data.Data.First().FirstName.Should().Be("John");
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        var request = new GetAllContactsQuery
        {
            Page = 1,
            PageSize = 10
        };

        var paginationResult = new Pagination<Contact>(new List<Contact>(), 1, 10, 0);

        _mockContactService.Setup(x => x.GetAllContactsAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();

    }

    [Fact]
    public async Task Handle_ServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new GetAllContactsQuery
        {
            Page = 1,
            PageSize = 10
        };

        _mockContactService.Setup(x => x.GetAllContactsAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(AppMessage.UnexpectedError.GetMessage()));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.UnexpectedError.GetMessage());
    }

    [Fact]
    public async Task Handle_LargePageRequest_ShouldHandleCorrectly()
    {
        // Arrange
        var request = new GetAllContactsQuery
        {
            Page = 5,
            PageSize = 20
        };

        var contacts = new List<Contact>
        {
            new("John", "Doe", "Tech Corp")
        };

        var paginationResult = new Pagination<Contact>(contacts, 5, 20, 81);

        _mockContactService.Setup(x => x.GetAllContactsAsync(5, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.PageNumber.Should().Be(5);
        result.Data.PageSize.Should().Be(20);
        result.Data.TotalCount.Should().Be(81);
        result.Data.TotalPages.Should().Be(5);
    }
}
