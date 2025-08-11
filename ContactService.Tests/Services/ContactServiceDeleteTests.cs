using ContactService.ApplicationService.Services;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ContactService.Tests.Services;

[Trait("Category", "Unit")]
public class ContactServiceDeleteTests
{
    private readonly Mock<IContactRepository> _contactRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<IContactHistoryService> _contactHistoryServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ApplicationService.Services.ContactService _contactService;

    public ContactServiceDeleteTests()
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
    public async Task DeleteContactAsync_WithExistingContact_ShouldDeleteSuccessfully()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John", "Doe", "Test Company");
        typeof(Contact).GetProperty("Id")?.SetValue(existingContact, contactId);

        // Add some contact info to test deletion
        existingContact.AddContactInfo(ContactInfoType.PhoneNumber, "+1234567890");
        existingContact.AddContactInfo(ContactInfoType.EmailAddress, "john@test.com");

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _contactRepositoryMock.Setup(x => x.DeleteAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _outboxServiceMock.Setup(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contactHistoryServiceMock.Setup(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _contactService.DeleteContactAsync(contactId);

        // Assert
        result.Should().BeTrue();
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.DeleteAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _outboxServiceMock.Verify(x => x.AddEventAsync("ContactDeleted", It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _contactHistoryServiceMock.Verify(x => x.RecordContactHistoryAsync(contactId, "DELETE", It.IsAny<object>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
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
        _outboxServiceMock.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _contactHistoryServiceMock.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteContactAsync_WithEmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act & Assert
        await _contactService.Invoking(x => x.DeleteContactAsync(emptyId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Contact ID cannot be empty. (Parameter 'id')");

        _contactRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _contactRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteContactAsync_WhenRepositoryDeleteFails_ShouldReturnFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = new Contact("John", "Doe", "Test Company");
        typeof(Contact).GetProperty("Id")?.SetValue(existingContact, contactId);

        _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        _contactRepositoryMock.Setup(x => x.DeleteAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Repository delete operation fails

        // Act
        var result = await _contactService.DeleteContactAsync(contactId);

        // Assert
        result.Should().BeFalse();
        _contactRepositoryMock.Verify(x => x.GetByIdAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        _contactRepositoryMock.Verify(x => x.DeleteAsync(contactId, It.IsAny<CancellationToken>()), Times.Once);
        // Events should not be published if delete fails
        _outboxServiceMock.Verify(x => x.AddEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _contactHistoryServiceMock.Verify(x => x.RecordContactHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
