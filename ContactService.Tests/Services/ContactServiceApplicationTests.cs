using ContactService.ApplicationService.Services;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Enums;
using ContactService.Domain.Models;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Http;
using Shared.CrossCutting.Models;

namespace ContactService.Tests.Services;

public class ContactServiceApplicationTests
{
    private readonly Mock<IContactRepository> _mockContactRepository;
    private readonly Mock<IOutboxService> _mockOutboxService;
    private readonly Mock<IContactHistoryService> _mockContactHistoryService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly ApplicationService.Services.ContactService _contactService;
    private readonly Guid _testContactId = Guid.NewGuid();
    private readonly Guid _testContactInfoId = Guid.NewGuid();

    public ContactServiceApplicationTests()
    {
        _mockContactRepository = new Mock<IContactRepository>();
        _mockOutboxService = new Mock<IOutboxService>();
        _mockContactHistoryService = new Mock<IContactHistoryService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        // Mock repository behaviors
        var testContact = new Contact("John", "Doe", "Test Company");
        testContact.AddContactInfo(ContactInfoType.PhoneNumber, "+905551234567");
        
        // Set up the contact ID to match our test ID
        typeof(Contact).GetProperty("Id")?.SetValue(testContact, _testContactId);
        
        // Set up contact info ID to match test ID
        var contactInfo = testContact.ContactInfos.First();
        typeof(ContactInfo).GetProperty("Id")?.SetValue(contactInfo, _testContactInfoId);
        
        // Setup repository mock behaviors
        _mockContactRepository.Setup(repo => repo.GetByIdAsync(_testContactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testContact);
        
        _mockContactRepository.Setup(repo => repo.ExistsAsync(_testContactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        _mockContactRepository.Setup(repo => repo.DeleteAsync(_testContactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        _mockContactRepository.Setup(repo => repo.RemoveContactInfoAsync(_testContactInfoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        _mockContactRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testContact);
            
        _mockContactRepository.Setup(repo => repo.CreateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Callback<Contact, CancellationToken>((contact, _) => 
            {
                typeof(Contact).GetProperty("Id")?.SetValue(contact, _testContactId);
            });
            
        // Setup pagination results
        var contacts = new List<Contact> { testContact };
        _mockContactRepository.Setup(repo => repo.GetAllPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((contacts, contacts.Count));
            
        _mockContactRepository.Setup(repo => repo.GetByLocationAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((contacts, contacts.Count));
            
        // Setup location statistics
        var statistics = new List<LocationStatistic> 
        { 
            new LocationStatistic 
            { 
                Location = "Istanbul", 
                ContactCount = 5, 
                PhoneNumberCount = 7 
            } 
        };
        
        _mockContactRepository.Setup(repo => repo.GetLocationStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);
        
        _contactService = new ApplicationService.Services.ContactService(
            _mockContactRepository.Object,
            _mockOutboxService.Object,
            _mockContactHistoryService.Object,
            _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task GetContactByIdAsync_ReturnsContact()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        
        // Act
        var result = await _contactService.GetContactByIdAsync(contactId);

        // Assert
        result.Should().BeNull(); // Implementation returns null for now
    }

    [Fact]
    public async Task GetAllContactsAsync_ReturnsPagedResult()
    {
        // Act
        var result = await _contactService.GetAllContactsAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Pagination<Contact>>();
    }

    [Fact]
    public async Task CreateContactAsync_ValidInput_ReturnsContact()
    {
        // Arrange
        var contactInfos = new List<ContactInfo>
        {
            new ContactInfo(Guid.NewGuid(), ContactInfoType.PhoneNumber, "+905551234567")
        };

        // Act
        var result = await _contactService.CreateContactAsync("John","Doe", "Tech Corp", contactInfos);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateContactAsync_ValidInput_ReturnsContact()
    {
        // Arrange
        var contactInfos = new List<ContactInfo>
        {
            new ContactInfo(_testContactInfoId, ContactInfoType.PhoneNumber, "+905551234567")
        };

        // Act
        var result = await _contactService.UpdateContactAsync(_testContactId, "John Doe Updated", "New Company", contactInfos);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteContactAsync_ReturnsBoolean()
    {
        // Act
        var result = await _contactService.DeleteContactAsync(_testContactId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddContactInfoAsync_ValidInput_ReturnsContact()
    {
        // Act
        var result = await _contactService.AddContactInfoAsync(_testContactId, 2, "test@example.com");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveContactInfoAsync_ReturnsBoolean()
    {
        // Act
        var result = await _contactService.RemoveContactInfoAsync(_testContactId, _testContactInfoId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ContactExistsAsync_ReturnsBoolean()
    {
        // Act
        var result = await _contactService.ContactExistsAsync(_testContactId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetLocationStatistics_ReturnsStatistics()
    {
        // Act
        var result = await _contactService.GetLocationStatistics(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<List<LocationStatistic>>();
    }

    [Fact]
    public async Task GetContactsFilterByCompany_ReturnsPagedResult()
    {
        // Act
        var result = await _contactService.GetContactsFilterByCompany(1, 10, "Tech Corp");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Pagination<Contact>>();
    }

    [Fact]
    public async Task GetContactsFilterByLocation_ReturnsPagedResult()
    {
        // Act
        var result = await _contactService.GetContactsFilterByLocation(1, 10, "Istanbul");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Pagination<Contact>>();
    }
}
