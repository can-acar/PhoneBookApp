using ContactService.ApplicationService.Handlers.Commands;
using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Contracts;
using ContactService.Domain;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace ContactService.Tests.Handlers.Commands;

public class UpdateContactHandlerTests
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly UpdateContactHandler _handler;

    public UpdateContactHandlerTests()
    {
        _mockContactService = new Mock<IContactService>();
        _handler = new UpdateContactHandler(_mockContactService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new UpdateContactCommand
        {
            Id = contactId,
            FirstName = "John",
            LastName = "Updated",
            Company = "Updated Corp",
            ContactInfos = new List<UpdateContactInfoDto>
            {
                new() { InfoType = (int)ContactInfoType.PhoneNumber, InfoValue = "+905551234567" }
            }
        };

        var contactInfos = new List<ContactInfo>
        {
            new(contactId, ContactInfoType.PhoneNumber, "+905551234567")
        };

        var updatedContact = new Contact("John","Updated", "Updated Corp");
        
        _mockContactService.Setup(x => x.UpdateContactAsync(It.IsAny<Guid>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(), It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedContact);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_EmptyFirstName_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new UpdateContactCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "",
            LastName = "Doe",
            Company = "Tech Corp"
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.UpdateContactFailed.GetMessage());
    }

    [Fact]
    public async Task Handle_EmptyLastName_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new UpdateContactCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "",
            Company = "Tech Corp"
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.UpdateContactFailed.GetMessage());
    }

    [Fact]
    public async Task Handle_EmptyCompany_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new UpdateContactCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Company = ""
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.UpdateContactFailed.GetMessage());
    }

    [Fact]
    public async Task Handle_EmptyId_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new UpdateContactCommand
        {
            Id = Guid.Empty,
            FirstName = "John",
            LastName = "Doe",
            Company = "Tech Corp"
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.UpdateContactFailed.GetMessage());
    }

    [Fact]
    public async Task Handle_ServiceReturnsNull_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new UpdateContactCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Company = "Tech Corp"
        };

        _mockContactService.Setup(x => x.UpdateContactAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.UpdateContactFailed.GetMessage());
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new UpdateContactCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Company = "Tech Corp"
        };

        _mockContactService.Setup(x => x.UpdateContactAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(AppMessage.UnexpectedError.GetMessage()));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain(AppMessage.UnexpectedError.GetMessage());
    }

    [Fact]
    public async Task Handle_WithContactInfos_ShouldUpdateContactWithInfos()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new UpdateContactCommand
        {
            Id = contactId,
            FirstName = "John",
            LastName = "Doe",
            Company = "Tech Corp",
            ContactInfos = new List<UpdateContactInfoDto>
            {
                new() { InfoType = (int)ContactInfoType.PhoneNumber, InfoValue = "+905551234567" },
                new() { InfoType = (int)ContactInfoType.EmailAddress, InfoValue = "john@techcorp.com" },
                new() { InfoType = (int)ContactInfoType.Location, InfoValue = "Istanbul" }
            }
        };

        var updatedContact = new Contact("John Doe", "Tech Corp");
        _mockContactService.Setup(x => x.UpdateContactAsync(contactId, "John", "Doe", "Tech Corp", It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedContact);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        _mockContactService.Verify(x => x.UpdateContactAsync(contactId, "John", "Doe", "Tech Corp",
                It.Is<IEnumerable<ContactInfo>>(infos => infos.Count() == 3), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyContactInfos_ShouldUpdateContactWithoutInfos()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var request = new UpdateContactCommand
        {
            Id = contactId,
            FirstName = "John",
            LastName = "Doe",
            Company = "Tech Corp",
            ContactInfos = new List<UpdateContactInfoDto>()
        };

        var updatedContact = new Contact("John Doe", "Tech Corp");
        _mockContactService.Setup(x => x.UpdateContactAsync(contactId, "John", "Doe", "Tech Corp", It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedContact);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        _mockContactService.Verify(x => x.UpdateContactAsync(contactId, "John", "Doe", "Tech Corp",
                It.Is<IEnumerable<ContactInfo>>(infos => !infos.Any()), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}