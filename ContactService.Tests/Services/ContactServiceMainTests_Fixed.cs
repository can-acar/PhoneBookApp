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

public class ContactServiceMainTestsFixed
{
    private readonly Mock<IContactRepository> _mockContactRepository;
    private readonly Mock<IOutboxService> _mockOutboxService;
    private readonly Mock<IContactHistoryService> _mockContactHistoryService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly ContactService.ApplicationService.Services.ContactService _contactService;

    public ContactServiceMainTestsFixed()
    {
        _mockContactRepository = new Mock<IContactRepository>();
        _mockOutboxService = new Mock<IOutboxService>();
        _mockContactHistoryService = new Mock<IContactHistoryService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        _contactService = new ContactService.ApplicationService.Services.ContactService(
            _mockContactRepository.Object,
            _mockOutboxService.Object,
            _mockContactHistoryService.Object,
            _mockHttpContextAccessor.Object);
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
    public async Task GetAllContactsAsync_ShouldReturnContacts_WhenContactsExist()
    {
        // Arrange
        var contact = new Contact("John", "Doe", "Tech Corp");
        var contacts = new List<Contact> { contact };

        _mockContactRepository
            .Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((contacts, 1));

        // Act
        var result = await _contactService.GetAllContactsAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data.First().FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task UpdateContactAsync_WhenContactExists_ShouldUpdateContactAndAddToHistory()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John", "Doe", "Tech Corp");

        var updatedContactInfos = new List<ContactInfo>
        {
            new(contactId, ContactInfoType.PhoneNumber, "+905551234567"),
            new(contactId, ContactInfoType.EmailAddress, "john.doe@example.com")
        };

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _mockContactRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        // Act
        var result = await _contactService.UpdateContactAsync(contactId, "John Smith", "New Company", updatedContactInfos);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("John Smith");
        result.Company.Should().Be("New Company");

        // Verify outbox service called
        _mockOutboxService.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<ContactUpdatedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify history service called
        _mockContactHistoryService.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddContactInfoAsync_WhenContactExists_ShouldAddInfoAndAddToHistory()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John", "Doe", "Tech Corp");

        _mockContactRepository
            .Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _mockContactRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        // Act
        var result = await _contactService.AddContactInfoAsync(contactId, (int)ContactInfoType.PhoneNumber, "+905551234567");

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
    public async Task RemoveContactInfoAsync_WhenContactInfoExists_ShouldRemoveInfoAndAddToHistory()
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
}
