using ContactService.ApplicationService.Services;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Events;
using ContactService.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.CrossCutting.Models;
using Xunit;

namespace ContactService.Tests.Services;

public class ContactServiceMainTests
{
    private readonly Mock<IContactRepository> _mockContactRepository;
    private readonly Mock<IOutboxService> _mockOutboxService;
    private readonly Mock<IContactHistoryService> _mockContactHistoryService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly ApplicationService.Services.ContactService _contactService;

    public ContactServiceMainTests()
    {
        _mockContactRepository = new Mock<IContactRepository>();
        _mockOutboxService = new Mock<IOutboxService>();
        _mockContactHistoryService = new Mock<IContactHistoryService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        _contactService = new ApplicationService.Services.ContactService(
            _mockContactRepository.Object,
            _mockOutboxService.Object,
            _mockContactHistoryService.Object,
            _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task GetContactByIdAsync_WhenContactExists_ShouldReturnContact()
    {
        // Arrange
        var expectedContact = new Contact("John", "Doe", "Tech Corp");

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(expectedContact.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContact);

        // Act
        var result = await _contactService.GetContactByIdAsync(expectedContact.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedContact.Id);
        result.FirstName.Should().Be("John");
        result.Company.Should().Be("Tech Corp");
    }

    [Fact]
    public async Task GetContactByIdAsync_WhenContactNotExists_ShouldReturnNull()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.GetContactByIdAsync(contactId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllContactsAsync_WhenContactsExist_ShouldReturnPaginatedContacts()
    {
        // Arrange
        var contacts = new List<Contact>
        {
            new("John", "Doe", "Tech Corp"),
            new("Jane", "Smith", "Design Inc")
        };

        var paginatedResult = await Task.FromResult((contacts.AsEnumerable(), contacts.Count));


        _mockContactRepository
            .Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _contactService.GetAllContactsAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Data.First().FirstName.Should().Be("John");
        result.Data.Last().FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task CreateContactAsync_ShouldCreateContact_WhenValidDataProvided()
    {
        // Arrange
        var contactInfos = new List<ContactInfo>();

        // Act
        var result = await _contactService.CreateContactAsync("John","Doe", "Tech Corp", contactInfos);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Company.Should().Be("Tech Corp");
    }

    [Fact]
    public async Task UpdateContactAsync_WhenContactExists_ShouldUpdateContactAndAddToHistory()
    {
        // Arrange
        var existingContact = new Contact("John", "Doe", "Tech Corp");

        var updatedContactInfos = new List<ContactInfo>();
        updatedContactInfos.Add(new ContactInfo(existingContact.Id, ContactInfoType.PhoneNumber, "+905551234567"));
        updatedContactInfos.Add(new ContactInfo(existingContact.Id, ContactInfoType.EmailAddress, "john.doe@mail.com"));
        updatedContactInfos.Add(new ContactInfo(existingContact.Id, ContactInfoType.Location, "Istanbul"));

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(existingContact.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _mockContactRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        // Act
        var result = await _contactService.UpdateContactAsync(existingContact.Id, "John Smith", "New Company", updatedContactInfos);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingContact.Id);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Smith");
        result.Company.Should().Be("New Company");

        // Verify outbox service called
        _mockOutboxService.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<ContactUpdatedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify history service called
        _mockContactHistoryService.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateContactAsync_WhenContactNotExists_ShouldReturnNull()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfos = new List<ContactInfo>();

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.UpdateContactAsync(contactId, "John Smith", "New Company", contactInfos);

        // Assert
        result.Should().BeNull();

        // Verify services not called
        _mockOutboxService.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<ContactUpdatedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockContactHistoryService.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteContactAsync_WhenContactExists_ShouldDeleteContactAndAddToHistory()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John Doe", "Tech Corp");

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _mockContactRepository
            .Setup(x => x.DeleteAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _contactService.DeleteContactAsync(contactId);

        // Assert
        result.Should().BeTrue();

        // Verify outbox service called
        _mockOutboxService.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<ContactDeletedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify history service called
        _mockContactHistoryService.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteContactAsync_WhenContactNotExists_ShouldReturnFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.DeleteContactAsync(contactId);

        // Assert
        result.Should().BeFalse();

        // Verify services not called
        _mockOutboxService.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<ContactDeletedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockContactHistoryService.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddContactInfoAsync_WhenContactExists_ShouldAddInfoAndAddToHistory()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John Doe", "Tech Corp");
        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _mockContactRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        // Act
        var result = await _contactService.AddContactInfoAsync(contactId, 1, "+905551234567");

        // Assert
        result.Should().NotBeNull();
        result!.ContactInfos.Should().HaveCount(1);
        result.ContactInfos.First().InfoType.Should().Be(ContactInfoType.PhoneNumber);
        result.ContactInfos.First().Content.Should().Be("+905551234567");

        // Verify outbox service called
        _mockOutboxService.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<ContactInfoAddedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify history service called
        _mockContactHistoryService.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddContactInfoAsync_WhenContactNotExists_ShouldReturnNull()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.AddContactInfoAsync(contactId, 1, "+905551234567");

        // Assert
        result.Should().BeNull();

        // Verify services not called
        _mockOutboxService.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<ContactInfoAddedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockContactHistoryService.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveContactInfoAsync_WhenContactAndInfoExist_ShouldRemoveInfoAndReturnTrue()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfoId = Guid.NewGuid();

        _mockContactRepository
            .Setup(x => x.RemoveContactInfoAsync(contactInfoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _contactService.RemoveContactInfoAsync(contactId, contactInfoId);

        // Assert
        result.Should().BeTrue();

        // Verify repository service called
        _mockContactRepository.Verify(x => x.RemoveContactInfoAsync(contactInfoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveContactInfoAsync_WhenContactNotExists_ShouldReturnFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactInfoId = Guid.NewGuid();

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _contactService.RemoveContactInfoAsync(contactId, contactInfoId);

        // Assert
        result.Should().BeFalse();

        // Verify history service not called
        _mockContactHistoryService.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ContactExistsAsync_WhenContactExists_ShouldReturnTrue()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        _mockContactRepository
            .Setup(x => x.ExistsAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _contactService.ContactExistsAsync(contactId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ContactExistsAsync_WhenContactNotExists_ShouldReturnFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        _mockContactRepository
            .Setup(x => x.ExistsAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _contactService.ContactExistsAsync(contactId);

        // Assert
        result.Should().BeFalse();
    }
}