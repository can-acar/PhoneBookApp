using ContactService.ApplicationService.Handlers.Commands;
using ContactService.ApiContract.Request.Commands;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace ContactService.Tests.Handlers.Commands;

public class RemoveContactInfoHandlerTests
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly RemoveContactInfoHandler _handler;

    public RemoveContactInfoHandlerTests()
    {
        _mockContactService = new Mock<IContactService>();
        _handler = new RemoveContactInfoHandler(_mockContactService.Object);
    }

        [Fact]
    public async Task Handle_ValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new RemoveContactInfoCommand
        {
            ContactId = Guid.NewGuid(),
            ContactInfoId = Guid.NewGuid()
        };

        var contact = new Contact("John Doe", "Tech Corp");
        
        _mockContactService.Setup(x => x.GetContactByIdAsync(request.ContactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);
        _mockContactService.Setup(x => x.RemoveContactInfoAsync(request.ContactId, request.ContactInfoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

        [Fact]
    public async Task Handle_ContactNotFound_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new RemoveContactInfoCommand
        {
            ContactId = Guid.NewGuid(),
            ContactInfoId = Guid.NewGuid()
        };

        _mockContactService.Setup(x => x.GetContactByIdAsync(request.ContactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kişi bulunamadı");
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfoId = Guid.NewGuid();
        var request = new RemoveContactInfoCommand
        {
            ContactId = contactId,
            ContactInfoId = contactInfoId
        };

        _mockContactService.Setup(x => x.RemoveContactInfoAsync(contactId, contactInfoId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kişi bulunamadı");
    }

    [Fact]
    public async Task Handle_EmptyContactId_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new RemoveContactInfoCommand
        {
            ContactId = Guid.Empty,
            ContactInfoId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kişi bulunamadı");
    }

    [Fact]
    public async Task Handle_EmptyContactInfoId_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new RemoveContactInfoCommand
        {
            ContactId = Guid.NewGuid(),
            ContactInfoId = Guid.Empty
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kişi bulunamadı");
    }

    [Fact]
    public async Task Handle_SuccessfulRemoval_ShouldCallServiceOnce()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfoId = Guid.NewGuid();
        var request = new RemoveContactInfoCommand
        {
            ContactId = contactId,
            ContactInfoId = contactInfoId
        };

        var contact = new Contact("John","Doe", "Tech Corp");

        _mockContactService.Setup(x => x.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);
        
        _mockContactService.Setup(x => x.RemoveContactInfoAsync(contactId, contactInfoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        _mockContactService.Verify(x => x.RemoveContactInfoAsync(contactId, contactInfoId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
