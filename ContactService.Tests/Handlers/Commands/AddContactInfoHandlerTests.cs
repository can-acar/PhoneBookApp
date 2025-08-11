using ContactService.ApplicationService.Handlers.Commands;
using ContactService.ApiContract.Request.Commands;
using ContactService.Domain;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace ContactService.Tests.Handlers.Commands;

public class AddContactInfoHandlerTests
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly AddContactInfoHandler _handler;

    public AddContactInfoHandlerTests()
    {
        _mockContactService = new Mock<IContactService>();
        _handler = new AddContactInfoHandler(_mockContactService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new AddContactInfoCommand
        {
            ContactId = contactId,
            InfoType = (int)ContactInfoType.PhoneNumber,
            InfoValue = "+905551234567"
        };

        var contact = new Contact("John Doe", "Tech Corp");
        contact.GetType().GetProperty("Id")?.SetValue(contact, contactId);
        
        var updatedContact = new Contact("John Doe", "Tech Corp");
        updatedContact.GetType().GetProperty("Id")?.SetValue(updatedContact, contactId);
        updatedContact.AddContactInfo(ContactInfoType.PhoneNumber, "+905551234567");

        _mockContactService.Setup(x => x.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);
        _mockContactService.Setup(x => x.AddContactInfoAsync(contactId, (int)ContactInfoType.PhoneNumber, "+905551234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedContact);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.ContactId.Should().Be(contactId);
    }

    [Fact]
    public async Task Handle_ContactNotFound_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new AddContactInfoCommand
        {
            ContactId = Guid.NewGuid(),
            InfoType = (int)ContactInfoType.EmailAddress,
            InfoValue = "test@example.com"
        };

        _mockContactService.Setup(x => x.GetContactByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.ContactNotFound.GetMessage());
    }

    [Fact]
    public async Task Handle_EmailContactInfo_ShouldCreateCorrectContactInfo()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new AddContactInfoCommand
        {
            ContactId = contactId,
            InfoType = (int)ContactInfoType.EmailAddress,
            InfoValue = "user@example.com"
        };

        var contact = new Contact("Jane Smith", "Example Corp");
        var updatedContact = new Contact("Jane Smith", "Example Corp");
        updatedContact.AddContactInfo(ContactInfoType.EmailAddress, "user@example.com");

        _mockContactService.Setup(x => x.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);
        _mockContactService.Setup(x => x.AddContactInfoAsync(contactId, (int)ContactInfoType.EmailAddress, "user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedContact);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        _mockContactService.Verify(x => x.AddContactInfoAsync(contactId, (int)ContactInfoType.EmailAddress, "user@example.com", It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_LocationContactInfo_ShouldCreateCorrectContactInfo()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new AddContactInfoCommand
        {
            ContactId = contactId,
            InfoType = (int)ContactInfoType.Location,
            InfoValue = "Istanbul"
        };

        var contact = new Contact("Ali Veli", "Istanbul Corp");
        var updatedContact = new Contact("Ali Veli", "Istanbul Corp");
        updatedContact.AddContactInfo(ContactInfoType.Location, "Istanbul");

        _mockContactService.Setup(x => x.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);
        _mockContactService.Setup(x => x.AddContactInfoAsync(contactId, (int)ContactInfoType.Location, "Istanbul", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedContact);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        _mockContactService.Verify(x => x.AddContactInfoAsync(contactId, (int)ContactInfoType.Location, "Istanbul", It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new AddContactInfoCommand
        {
            ContactId = Guid.NewGuid(),
            InfoType = (int)ContactInfoType.PhoneNumber,
            InfoValue = "+905559876543"
        };

        var contact = new Contact("Test User", "Test Corp");
        _mockContactService.Setup(x => x.GetContactByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);
        _mockContactService.Setup(x => x.AddContactInfoAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Beklenmeyen bir hata oluştu");
    }
}
