using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactService.ApplicationService.Services;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Domain.Models;
using ContactService.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.CrossCutting.Models;
using Xunit;

namespace ContactService.Tests.Services
{
    public class CachedContactServiceTests
    {
        private readonly Mock<IContactServiceCore> _mockBaseService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IOptions<RedisSettings>> _mockRedisOptions;
        private readonly Mock<ILogger<CachedContactService>> _mockLogger;
        private readonly CachedContactService _cachedService;
        private readonly RedisSettings _redisSettings;

        public CachedContactServiceTests()
        {
            _mockBaseService = new Mock<IContactServiceCore>();
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<CachedContactService>>();

            _redisSettings = new RedisSettings
            {
                ConnectionString = "localhost:6379",
                ContactCacheExpiration = TimeSpan.FromMinutes(15),
                LocationStatsCacheExpiration = TimeSpan.FromHours(1)
            };

            _mockRedisOptions = new Mock<IOptions<RedisSettings>>();
            _mockRedisOptions.Setup(o => o.Value).Returns(_redisSettings);

            _cachedService = new CachedContactService(
                _mockBaseService.Object,
                _mockCacheService.Object,
                _mockRedisOptions.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetContactByIdAsync_WhenCached_ShouldReturnFromCache()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var cachedContact = new Contact("Test", "TestCompany");

            _mockCacheService
                .Setup(c => c.GetAsync<Contact>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedContact);

            // Act
            var result = await _cachedService.GetContactByIdAsync(contactId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(cachedContact);

            _mockCacheService.Verify(
                c => c.GetAsync<Contact>(It.Is<string>(s => s.Contains(contactId.ToString())), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBaseService.Verify(
                s => s.GetContactByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetContactByIdAsync_WhenNotCached_ShouldFetchAndCache()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var contact = new Contact("Test", "TestCompany");

            _mockCacheService
                .Setup(c => c.GetAsync<Contact>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contact)null);

            _mockBaseService
                .Setup(s => s.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            // Act
            var result = await _cachedService.GetContactByIdAsync(contactId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(contact);

            _mockCacheService.Verify(
                c => c.GetAsync<Contact>(It.Is<string>(s => s.Contains(contactId.ToString())), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBaseService.Verify(
                s => s.GetContactByIdAsync(contactId, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCacheService.Verify(
                c => c.SetAsync(
                    It.Is<string>(s => s.Contains(contactId.ToString())),
                    It.Is<Contact>(c => c == contact),
                    It.Is<TimeSpan?>(t => t == _redisSettings.ContactCacheExpiration),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllContactsAsync_WhenCached_ShouldReturnFromCache()
        {
            // Arrange
            int page = 1;
            int pageSize = 10;
            string searchTerm = "test";

            var contacts = new List<Contact> { new Contact("Test", "TestCompany") };
            var cachedContacts = new Pagination<Contact>(
                contacts,
                page,
                pageSize,
                contacts.Count);

            _mockCacheService
                .Setup(c => c.GetAsync<Pagination<Contact>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedContacts);

            // Act
            var result = await _cachedService.GetAllContactsAsync(page, pageSize, searchTerm);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(cachedContacts);

            _mockCacheService.Verify(
                c => c.GetAsync<Pagination<Contact>>(
                    It.Is<string>(s => s.Contains(page.ToString()) && s.Contains(pageSize.ToString()) && s.Contains(searchTerm)),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBaseService.Verify(
                s => s.GetAllContactsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllContactsAsync_WhenNotCached_ShouldFetchAndCache()
        {
            // Arrange
            int page = 1;
            int pageSize = 10;
            string searchTerm = "test";

            var contactsList = new List<Contact> { new Contact("Test", "TestCompany") };
            var contacts = new Pagination<Contact>(
                contactsList,
                page,
                pageSize,
                contactsList.Count);

            _mockCacheService
                .Setup(c => c.GetAsync<Pagination<Contact>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Pagination<Contact>)null);

            _mockBaseService
                .Setup(s => s.GetAllContactsAsync(page, pageSize, searchTerm, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contacts);

            // Act
            var result = await _cachedService.GetAllContactsAsync(page, pageSize, searchTerm);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(contacts);

            _mockCacheService.Verify(
                c => c.GetAsync<Pagination<Contact>>(
                    It.Is<string>(s => s.Contains(page.ToString()) && s.Contains(pageSize.ToString())),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBaseService.Verify(
                s => s.GetAllContactsAsync(page, pageSize, searchTerm, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCacheService.Verify(
                c => c.SetAsync(
                    It.Is<string>(s => s.Contains(page.ToString()) && s.Contains(pageSize.ToString())),
                    It.Is<Pagination<Contact>>(p => p == contacts),
                    It.Is<TimeSpan?>(t => t == _redisSettings.ContactCacheExpiration),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateContactAsync_ShouldInvalidateCache()
        {
            // Arrange
            var firstName = "Test";
            var lastName = "Name";
            var company = "Test Company";
            var contactInfos = new List<ContactInfo>();
            var createdContact = new Contact(firstName, company);

            _mockBaseService
                .Setup(s => s.CreateContactAsync(firstName, lastName, company, contactInfos, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdContact);

            // Act
            var result = await _cachedService.CreateContactAsync(firstName, lastName, company, contactInfos);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(createdContact);

            _mockBaseService.Verify(
                s => s.CreateContactAsync(firstName, lastName, company, contactInfos, It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify pattern-based cache invalidation was called
            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync(It.Is<string>(s => s.Contains("contact:")), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync(It.Is<string>(s => s.Contains("contacts:")), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateContactAsync_ShouldInvalidateCache()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var firstName = "Updated";
            var lastName = "Name";
            var company = "Updated Company";
            var contactInfos = new List<ContactInfo>();
            var updatedContact = new Contact(firstName, company);

            _mockBaseService
                .Setup(s => s.UpdateContactAsync(contactId, firstName, lastName, company, contactInfos, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedContact);

            // Act
            var result = await _cachedService.UpdateContactAsync(contactId, firstName, lastName, company, contactInfos);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(updatedContact);

            _mockBaseService.Verify(
                s => s.UpdateContactAsync(contactId, firstName, lastName, company, contactInfos, It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify specific key invalidation
            _mockCacheService.Verify(
                c => c.RemoveAsync(It.Is<string>(s => s.Contains(contactId.ToString())), It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify pattern-based cache invalidation
            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task DeleteContactAsync_ShouldInvalidateCache()
        {
            // Arrange
            var contactId = Guid.NewGuid();

            _mockBaseService
                .Setup(s => s.DeleteContactAsync(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _cachedService.DeleteContactAsync(contactId);

            // Assert
            result.Should().BeTrue();

            _mockBaseService.Verify(
                s => s.DeleteContactAsync(contactId, It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify specific key invalidation
            _mockCacheService.Verify(
                c => c.RemoveAsync(It.Is<string>(s => s.Contains(contactId.ToString())), It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify pattern-based cache invalidation
            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task AddContactInfoAsync_ShouldInvalidateCache()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var infoType = 1; // Phone
            var infoValue = "123456789";
            var updatedContact = new Contact("Test", "Test Company");

            _mockBaseService
                .Setup(s => s.AddContactInfoAsync(contactId, infoType, infoValue, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedContact);

            // Act
            var result = await _cachedService.AddContactInfoAsync(contactId, infoType, infoValue);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(updatedContact);

            _mockBaseService.Verify(
                s => s.AddContactInfoAsync(contactId, infoType, infoValue, It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify specific key invalidation
            _mockCacheService.Verify(
                c => c.RemoveAsync(It.Is<string>(s => s.Contains(contactId.ToString())), It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify pattern-based cache invalidation
            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task RemoveContactInfoAsync_ShouldInvalidateCache()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var contactInfoId = Guid.NewGuid();

            _mockBaseService
                .Setup(s => s.RemoveContactInfoAsync(contactId, contactInfoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _cachedService.RemoveContactInfoAsync(contactId, contactInfoId);

            // Assert
            result.Should().BeTrue();

            _mockBaseService.Verify(
                s => s.RemoveContactInfoAsync(contactId, contactInfoId, It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify specific key invalidation
            _mockCacheService.Verify(
                c => c.RemoveAsync(It.Is<string>(s => s.Contains(contactId.ToString())), It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify pattern-based cache invalidation
            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ContactExistsAsync_WhenCached_ShouldReturnTrue()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var cachedContact = new Contact("Test", "TestCompany");

            _mockCacheService
                .Setup(c => c.GetAsync<Contact>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedContact);

            // Act
            var result = await _cachedService.ContactExistsAsync(contactId);

            // Assert
            result.Should().BeTrue();

            _mockCacheService.Verify(
                c => c.GetAsync<Contact>(It.Is<string>(s => s.Contains(contactId.ToString())), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBaseService.Verify(
                s => s.ContactExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ContactExistsAsync_WhenNotCached_ShouldCheckBaseService()
        {
            // Arrange
            var contactId = Guid.NewGuid();

            _mockCacheService
                .Setup(c => c.GetAsync<Contact>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contact)null);

            _mockBaseService
                .Setup(s => s.ContactExistsAsync(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _cachedService.ContactExistsAsync(contactId);

            // Assert
            result.Should().BeTrue();

            _mockCacheService.Verify(
                c => c.GetAsync<Contact>(It.Is<string>(s => s.Contains(contactId.ToString())), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBaseService.Verify(
                s => s.ContactExistsAsync(contactId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetLocationStatistics_WhenCached_ShouldReturnFromCache()
        {
            // Arrange
            var cachedStats = new List<LocationStatistic>
            {
                new LocationStatistic { Location = "Istanbul", ContactCount = 10, PhoneNumberCount = 15 }
            };

            _mockCacheService
                .Setup(c => c.GetAsync<List<LocationStatistic>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedStats);

            // Act
            var result = await _cachedService.GetLocationStatistics(CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(cachedStats);

            _mockCacheService.Verify(
                c => c.GetAsync<List<LocationStatistic>>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBaseService.Verify(
                s => s.GetLocationStatistics(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetLocationStatistics_WhenNotCached_ShouldFetchAndCache()
        {
            // Arrange
            var stats = new List<LocationStatistic>
            {
                new LocationStatistic { Location = "Istanbul", ContactCount = 10, PhoneNumberCount = 15 }
            };

            _mockCacheService
                .Setup(c => c.GetAsync<List<LocationStatistic>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<LocationStatistic>)null);

            _mockBaseService
                .Setup(s => s.GetLocationStatistics(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stats);

            // Act
            var result = await _cachedService.GetLocationStatistics(CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(stats);

            _mockCacheService.Verify(
                c => c.GetAsync<List<LocationStatistic>>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBaseService.Verify(
                s => s.GetLocationStatistics(It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCacheService.Verify(
                c => c.SetAsync(
                    It.IsAny<string>(),
                    It.Is<List<LocationStatistic>>(l => l == stats),
                    It.Is<TimeSpan?>(t => t == _redisSettings.LocationStatsCacheExpiration),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}