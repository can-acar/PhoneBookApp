using ContactService.ApplicationService.Handlers.Commands;
using ContactService.ApiContract.Request.Commands;
using ContactService.Domain;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Entities;
using FluentAssertions;
using Moq;
using Shared.CrossCutting.Models;
using Xunit;

namespace ContactService.Tests.Handlers.Commands;

public class DeleteContactHandlerTests
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly DeleteContactHandler _handler;

    public DeleteContactHandlerTests()
    {
        _mockContactService = new Mock<IContactService>();
        _handler = new DeleteContactHandler(_mockContactService.Object);
    }

    [Fact]
    public async Task Handle_ValidContactId_ShouldReturnSuccessResponse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new DeleteContactCommand { Id = contactId };
        var contact = new Contact("John Doe", "Tech Corp");

        _mockContactService.Setup(x => x.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);
        _mockContactService.Setup(x => x.DeleteContactAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ContactNotFound_ShouldReturnFailureResponse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new DeleteContactCommand { Id = contactId };


        _mockContactService.Setup(x => x.DeleteContactAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.ContactNotFound.GetMessage());
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new DeleteContactCommand { Id = contactId };
        var contact = new Contact("Howard", "Stark", "Stark Industries");
        contact.GetType().GetProperty("Id")?.SetValue(contact, Guid.NewGuid(), null);

        _mockContactService.Setup(x => x.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact); // Simulate contact not found

        _mockContactService.Setup(x => x.DeleteContactAsync(contactId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(AppMessage.UnexpectedError.GetMessage()));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.UnexpectedError.GetMessage());
    }

    [Fact]
    public async Task Handle_EmptyGuid_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new DeleteContactCommand { Id = Guid.Empty };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.ContactNotFound.GetMessage());
    }

    [Fact]
    public async Task Handle_SuccessfulDeletion_ShouldCallServiceOnce()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new DeleteContactCommand { Id = contactId };

        var contact = new Contact("John Doe", "Tech Corp");

        _mockContactService.Setup(x => x.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);
        _mockContactService.Setup(x => x.DeleteContactAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        _mockContactService.Verify(x => x.DeleteContactAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
    }
}