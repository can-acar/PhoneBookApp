using ContactService.ApplicationService.Services;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Shared.CrossCutting.Models;

namespace ContactService.Tests.Services;

[Trait("Category", "Unit")]
public class ContactServiceTestsFixed
{
    private readonly Mock<IContactRepository> _contactRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<IContactHistoryService> _contactHistoryServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ApplicationService.Services.ContactService _contactService;

    public ContactServiceTestsFixed()
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
    public async Task GetContactByIdAsync_ExistingId_ShouldReturnContact()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = new Contact("John", "Doe", "Test Company");

        // Set Id property using reflection for testing
        typeof(Contact).GetProperty("Id")?.SetValue(contact, contactId);

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        // Act
        var result = await _contactService.GetContactByIdAsync(contactId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(contactId);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Company.Should().Be("Test Company");
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetContactByIdAsync_NonExistingId_ShouldReturnNull()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.GetContactByIdAsync(contactId);

        // Assert
        result.Should().BeNull();
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllContactsAsync_ShouldReturnAllContacts()
    {
        // Arrange
        var contacts = new List<Contact>
        {
            new("John", "Doe", "Company1"),
            new("Jane", "Smith", "Company2")
        };

        _contactRepositoryMock.Setup(x => x.GetAllPagedAsync(1, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((contacts, 2));

        // Act
        var result = await _contactService.GetAllContactsAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Data.Should().BeEquivalentTo(contacts);
        _contactRepositoryMock.Verify(x => x.GetAllPagedAsync(1, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchContactsAsync_WithSearchTerm_ShouldReturnMatchingContacts()
    {
        // Arrange
        var searchTerm = "John";
        var contacts = new List<Contact>
        {
            new("John", "Doe", "Company1")
        };

        _contactRepositoryMock.Setup(x => x.GetAllPagedAsync(1, 10, searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync((contacts, 1));

        // Act
        var result = await _contactService.GetAllContactsAsync(1, 10, searchTerm);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data.First().FirstName.Should().Be("John");
        _contactRepositoryMock.Verify(x => x.GetAllPagedAsync(1, 10, searchTerm, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetContactsByLocationAsync_WithLocation_ShouldReturnContactsFromLocation()
    {
        // Arrange
        var location = "Istanbul";
        var contacts = new List<Contact>
        {
            new("John", "Doe", "Company1")
        };

        _contactRepositoryMock.Setup(x => x.GetByLocationAsync(1, 10, location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((contacts, 1));

        // Act
        var result = await _contactService.GetContactsFilterByLocation(1, 10, location);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        _contactRepositoryMock.Verify(x => x.GetByLocationAsync(1, 10, location, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateContactAsync_WithValidData_ShouldCreateContact()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var company = "Test Company";
        var contactInfos = new List<ContactInfo>
        {
            new(Guid.NewGuid(), ContactInfoType.PhoneNumber, "+1234567890"),
            new(Guid.NewGuid(), ContactInfoType.EmailAddress, "john@test.com")
        };

        Contact savedContact = null!;
        _contactRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Callback<Contact, CancellationToken>((contact, _) => savedContact = contact)
            .ReturnsAsync((Contact contact, CancellationToken _) => contact);

        // Act
        var result = await _contactService.CreateContactAsync(firstName, lastName, company, contactInfos);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Company.Should().Be(company);
        result.ContactInfos.Should().HaveCount(2);
        _contactRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateContactAsync_WithValidData_ShouldUpdateContact()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John", "Doe", "Old Company");
        typeof(Contact).GetProperty("Id")?.SetValue(existingContact, contactId);

        var newContactInfos = new List<ContactInfo>
        {
            new(Guid.NewGuid(), ContactInfoType.PhoneNumber, "+1234567890")
        };

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        Contact savedContact = null!;
        _contactRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Callback<Contact, CancellationToken>((contact, _) => savedContact = contact)
            .ReturnsAsync((Contact contact, CancellationToken _) => contact);

        // Act
        var result = await _contactService.UpdateContactAsync(contactId, "Jane Smith", "New Company", newContactInfos);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.Company.Should().Be("New Company");
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task DeleteContactAsync_WithExistingContact_ShouldDeleteContact()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John", "Doe", "Test Company");
        typeof(Contact).GetProperty("Id")?.SetValue(existingContact, contactId);

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _contactRepositoryMock.Setup(x => x.DeleteAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _contactService.DeleteContactAsync(contactId);

        // Assert
        result.Should().BeTrue();
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.DeleteAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteContactAsync_WithNonExistingContact_ShouldReturnFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.DeleteContactAsync(contactId);

        // Assert
        result.Should().BeFalse();
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddContactInfoAsync_WithValidData_ShouldAddContactInfo()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfoId = Guid.NewGuid();
        var contact = new Contact("John", "Doe", "Test Company");
        var contactInfo = new ContactInfo(Guid.NewGuid(), ContactInfoType.PhoneNumber, "+1234567890");

        typeof(Contact).GetProperty("Id")?.SetValue(contact, contactId);
        typeof(ContactInfo).GetProperty("Id")?.SetValue(contactInfo, contactInfoId);

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _contactRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact c, CancellationToken _) => c);

        // Act
        var result = await _contactService.AddContactInfoAsync(contactId, (int)ContactInfoType.PhoneNumber, "+1234567890");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(contactId);
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
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
    }

    [Fact]
    public async Task RemoveContactInfoAsync_WithValidData_ShouldRemoveContactInfo()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfoId = Guid.NewGuid();

        _contactRepositoryMock.Setup(x => x.RemoveContactInfoAsync(contactInfoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _contactService.RemoveContactInfoAsync(contactId, contactInfoId);

        // Assert
        result.Should().BeTrue();
        _contactRepositoryMock.Verify(x => x.RemoveContactInfoAsync(contactInfoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveContactInfoAsync_WithNonExistingContact_ShouldReturnFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfoId = Guid.NewGuid();

        _contactRepositoryMock.Setup(x => x.RemoveContactInfoAsync(contactInfoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _contactService.RemoveContactInfoAsync(contactId, contactInfoId);

        // Assert
        result.Should().BeFalse();
        _contactRepositoryMock.Verify(x => x.RemoveContactInfoAsync(contactInfoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveContactInfoAsync_WithNonExistingContactInfo_ShouldReturnFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfoId = Guid.NewGuid();

        _contactRepositoryMock.Setup(x => x.RemoveContactInfoAsync(contactInfoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _contactService.RemoveContactInfoAsync(contactId, contactInfoId);

        // Assert
        result.Should().BeFalse();
        _contactRepositoryMock.Verify(x => x.RemoveContactInfoAsync(contactInfoId, It.IsAny<CancellationToken>()), Times.Once);
    }
}