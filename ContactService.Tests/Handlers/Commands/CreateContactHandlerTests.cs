using ContactService.ApplicationService.Handlers.Commands;
using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Contracts;
using ContactService.Domain;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using FluentAssertions;
using Moq;
using Shared.CrossCutting.Models;
using Xunit;

namespace ContactService.Tests.Handlers.Commands;

public class CreateContactHandlerTests
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly CreateContactHandler _handler;

    public CreateContactHandlerTests()
    {
        _mockContactService = new Mock<IContactService>();
        _handler = new CreateContactHandler(_mockContactService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new CreateContactCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Company = "Tech Corp",
            ContactInfos = new List<CreateContactInfoDto>
            {
                new() { InfoType = (int)ContactInfoType.PhoneNumber, InfoValue = "+905551234567" },
                new() { InfoType = (int)ContactInfoType.EmailAddress, InfoValue = "john@techcorp.com" }
            }
        };

        var contactInfos = new List<ContactInfo>
        {
            new(Guid.NewGuid(), ContactInfoType.PhoneNumber, "+905551234567"),
            new(Guid.NewGuid(), ContactInfoType.EmailAddress, "john@techcorp.com")
        };

        var createdContact = new Contact("John Doe", "Tech Corp");
        _mockContactService.Setup(x => x.CreateContactAsync("John", "Doe", "Tech Corp", It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdContact);

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
        var command = new CreateContactCommand
        {
            FirstName = "",
            Company = "Test Company",
            ContactInfos = new List<CreateContactInfoDto>()
        };

        _mockContactService.Setup(x => x.CreateContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kişi oluşturma işlemi başarısız oldu");
    }

    [Fact]
    public async Task Handle_EmptyLastName_ShouldReturnErrorResponse()
    {
        // Arrange
        var command = new CreateContactCommand
        {
            FirstName = "Test",
            Company = "",
            ContactInfos = new List<CreateContactInfoDto>()
        };

        _mockContactService.Setup(x => x.CreateContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kişi oluşturma işlemi başarısız oldu");
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new CreateContactCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Company = "Tech Corp",
            ContactInfos = new List<CreateContactInfoDto>()
        };

        _mockContactService.Setup(x => x.CreateContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Beklenmeyen bir hata oluştu");
    }

    [Fact]
    public async Task Handle_EmptyContactInfos_ShouldCreateContactSuccessfully()
    {
        // Arrange
        var request = new CreateContactCommand
        {
            FirstName = "Jane",
            LastName = "Smith",
            Company = "Example Corp",
            ContactInfos = new List<CreateContactInfoDto>()
        };

        var createdContact = new Contact("Jane", "Smith", "Example Corp");

        _mockContactService.Setup(x => x.CreateContactAsync("Jane", "Smith", "Example Corp", It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdContact);


        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_MultipleContactInfos_ShouldCreateContactWithAllInfos()
    {
        // Arrange
        var request = new CreateContactCommand
        {
            FirstName = "Ali",
            LastName = "Veli",
            Company = "Istanbul Tech",
            ContactInfos = new List<CreateContactInfoDto>
            {
                new() { InfoType = (int)ContactInfoType.PhoneNumber, InfoValue = "+905551111111" },
                new() { InfoType = (int)ContactInfoType.EmailAddress, InfoValue = "ali@istanbul.com" },
                new() { InfoType = (int)ContactInfoType.Location, InfoValue = "Istanbul" }
            }
        };

        var createdContact = new Contact("Ali Veli", "Istanbul Tech");
        _mockContactService.Setup(x => x.CreateContactAsync("Ali", "Veli", "Istanbul Tech", It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdContact);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        _mockContactService.Verify(x => x.CreateContactAsync("Ali", "Veli", "Istanbul Tech",
                It.Is<IEnumerable<ContactInfo>>(infos => infos.Count() == 3), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceReturnsNull_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new CreateContactCommand
        {
            FirstName = "Test",
            LastName = "User",
            Company = "Test Corp",
            ContactInfos = new List<CreateContactInfoDto>()
        };

        _mockContactService.Setup(x => x.CreateContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ContactInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kişi oluşturma işlemi başarısız oldu");
    }
}