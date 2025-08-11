using ContactService.ApplicationService.Services;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ContactService.Tests.Services;

[Trait("Category", "Unit")]
public class ContactServiceAddContactInfoTests
{
    private readonly Mock<IContactRepository> _contactRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<IContactHistoryService> _contactHistoryServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ApplicationService.Services.ContactService _contactService;

    public ContactServiceAddContactInfoTests()
    {
        _contactRepositoryMock = new Mock<IContactRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _contactHistoryServiceMock = new Mock<IContactHistoryService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _contactService = new ApplicationService.Services.ContactService(
            _contactRepositoryMock.Object,
            _outboxServiceMock.Object,
            _contactHistoryServiceMock.Object,
            _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task AddContactInfoAsync_WithValidData_ShouldAddContactInfoSuccessfully()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = new Contact("John", "Doe", "Test Company");
        typeof(Contact).GetProperty("Id")?.SetValue(contact, contactId);

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _contactRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact c, CancellationToken _) => c);

        _outboxServiceMock.Setup(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contactHistoryServiceMock.Setup(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _contactService.AddContactInfoAsync(contactId, (int)ContactInfoType.PhoneNumber, "+1234567890");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(contactId);
        result.ContactInfos.Should().HaveCount(1);
        result.ContactInfos.First().InfoType.Should().Be(ContactInfoType.PhoneNumber);
        result.ContactInfos.First().Content.Should().Be("+1234567890");

        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxServiceMock.Verify(x => x.AddEventAsync("ContactInfoAdded", It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _contactHistoryServiceMock.Verify(x => x.RecordContactHistoryAsync(contactId, "ADD_CONTACT_INFO", It.IsAny<object>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddContactInfoAsync_WithNonExistingContact_ShouldReturnNull()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.AddContactInfoAsync(contactId, (int)ContactInfoType.PhoneNumber, "+1234567890");

        // Assert
        result.Should().BeNull();
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxServiceMock.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _contactHistoryServiceMock.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddContactInfoAsync_WithEmptyContactId_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act & Assert
        await _contactService.Invoking(x => x.AddContactInfoAsync(emptyId, (int)ContactInfoType.PhoneNumber, "+1234567890"))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Contact ID cannot be empty. (Parameter 'contactId')");

        _contactRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddContactInfoAsync_WithEmptyContent_ShouldThrowArgumentException()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        // Act & Assert
        await _contactService.Invoking(x => x.AddContactInfoAsync(contactId, (int)ContactInfoType.PhoneNumber, ""))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Contact info value cannot be empty. (Parameter 'infoValue')");

        _contactRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddContactInfoAsync_WithNullContent_ShouldThrowArgumentException()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        // Act & Assert
        await _contactService.Invoking(x => x.AddContactInfoAsync(contactId, (int)ContactInfoType.PhoneNumber, null!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Contact info value cannot be empty. (Parameter 'infoValue')");

        _contactRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddContactInfoAsync_WithEmailType_ShouldAddEmailContactInfo()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = new Contact("John", "Doe", "Test Company");
        typeof(Contact).GetProperty("Id")?.SetValue(contact, contactId);

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _contactRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact c, CancellationToken _) => c);

        _outboxServiceMock.Setup(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contactHistoryServiceMock.Setup(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _contactService.AddContactInfoAsync(contactId, (int)ContactInfoType.EmailAddress, "john@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.ContactInfos.Should().HaveCount(1);
        result.ContactInfos.First().InfoType.Should().Be(ContactInfoType.EmailAddress);
        result.ContactInfos.First().Content.Should().Be("john@test.com");
    }

    [Fact]
    public async Task AddContactInfoAsync_WithLocationInfo_ShouldAddLocationContactInfo()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = new Contact("John", "Doe", "Test Company");
        typeof(Contact).GetProperty("Id")?.SetValue(contact, contactId);

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _contactRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact c, CancellationToken _) => c);

        _outboxServiceMock.Setup(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contactHistoryServiceMock.Setup(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())) 
            .Returns(Task.CompletedTask);

        // Act
        var result = await _contactService.AddContactInfoAsync(contactId, (int)ContactInfoType.Location, "Istanbul");

        // Assert
        result.Should().NotBeNull();
        result!.ContactInfos.Should().HaveCount(1);
        result.ContactInfos.First().InfoType.Should().Be(ContactInfoType.Location);
        result.ContactInfos.First().Content.Should().Be("Istanbul");
    }
}
