using ContactService.ApplicationService.Services;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Shared.CrossCutting.Models;

namespace ContactService.Tests.Services;

[Trait("Category", "Unit")]
public class ContactServiceUpdateTests
{
    private readonly Mock<IContactRepository> _contactRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<IContactHistoryService> _contactHistoryServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ApplicationService.Services.ContactService _contactService;

    public ContactServiceUpdateTests()
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
    public async Task UpdateContactAsync_WithValidData_ShouldUpdateContactSuccessfully()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John", "Doe", "Old Company");
        typeof(Contact).GetProperty("Id")?.SetValue(existingContact, contactId);

        var newContactInfos = new List<ContactInfo>
        {
            new(Guid.NewGuid(), ContactInfoType.PhoneNumber, "+1234567890"),
            new(Guid.NewGuid(), ContactInfoType.EmailAddress, "john@newcompany.com")
        };

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _contactRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact contact, CancellationToken _) => contact);

        _outboxServiceMock.Setup(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contactHistoryServiceMock.Setup(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _contactService.UpdateContactAsync(contactId, "Jane Smith", "New Company", newContactInfos);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.Company.Should().Be("New Company");
        
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxServiceMock.Verify(x => x.AddEventAsync("ContactUpdated", It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _contactHistoryServiceMock.Verify(x => x.RecordContactHistoryAsync(contactId, "UPDATE", It.IsAny<object>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateContactAsync_WithEmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act & Assert
        await _contactService.Invoking(x => x.UpdateContactAsync(emptyId, "Jane Smith", "Company", new List<ContactInfo>()))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Contact ID cannot be empty. (Parameter 'id')");

        _contactRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateContactAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        // Act & Assert
        await _contactService.Invoking(x => x.UpdateContactAsync(contactId, "", "Company", new List<ContactInfo>()))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Contact name cannot be empty. (Parameter 'name')");

        _contactRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateContactAsync_WithNonExistingContact_ShouldReturnNull()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.UpdateContactAsync(contactId, "Jane Smith", "Company", new List<ContactInfo>());

        // Assert
        result.Should().BeNull();
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateContactAsync_WithSingleName_ShouldParseCorrectly()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John", "Doe", "Old Company");
        typeof(Contact).GetProperty("Id")?.SetValue(existingContact, contactId);

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _contactRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact contact, CancellationToken _) => contact);

        _outboxServiceMock.Setup(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contactHistoryServiceMock.Setup(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _contactService.UpdateContactAsync(contactId, "Jane Smith", "New Company", new List<ContactInfo>());

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.Company.Should().Be("New Company");
    }
}
