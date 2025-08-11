using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Data;
using ContactService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContactService.Tests.Repositories;

public class ContactHistoryRepositoryTests
{
    private readonly DbContextOptions<ContactDbContext> _dbContextOptions;
    
    public ContactHistoryRepositoryTests()
    {
        // Use in-memory database for testing
        _dbContextOptions = new DbContextOptionsBuilder<ContactDbContext>()
            .UseInMemoryDatabase(databaseName: $"ContactDb_{Guid.NewGuid()}")
            .Options;
    }
    
    [Fact]
    public async Task CreateAsync_ShouldAddHistoryToDatabase()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new ContactHistoryRepository(context);
        
        var contactHistory = new ContactHistory(
            Guid.NewGuid(),
            "CREATE",
            "{\"id\":\"test-id\", \"firstName\":\"John\"}",
            Guid.NewGuid().ToString());
        
        // Act
        var result = await repository.CreateAsync(contactHistory);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(contactHistory.Id);
        
        // Verify it was saved to database
        var savedHistory = await context.ContactHistories
            .FirstOrDefaultAsync(h => h.Id == contactHistory.Id);
        savedHistory.Should().NotBeNull();
        savedHistory!.OperationType.Should().Be("CREATE");
    }
    
    [Fact]
    public async Task CreateAsync_WithNullHistory_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new ContactHistoryRepository(context);
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            repository.CreateAsync(null!));
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenHistoryExists_ShouldReturnHistory()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new ContactHistoryRepository(context);
        
        var contactId = Guid.NewGuid();
        // Create contact history with its constructor
        var history = new ContactHistory(
            contactId, 
            "UPDATE", 
            new { id = "test-id", firstName = "John" }, 
            "test-correlation");
        
        // Set id field to known value for test
        // Note: In real code, consider refactoring to expose Id as a parameter in constructor
        typeof(ContactHistory).GetProperty("Id")?.SetValue(history, historyId);
        
        context.ContactHistories.Add(history);
        await context.SaveChangesAsync();
        
        // Act
        var result = await repository.GetByIdAsync(historyId);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(historyId);
        result.OperationType.Should().Be("UPDATE");
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenHistoryDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new ContactHistoryRepository(context);
        
        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetByContactIdAsync_ShouldReturnMatchingHistories()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedHistoryRecordsAsync(context, contactId, 3);
        var repository = new ContactHistoryRepository(context);

        // Act
        var results = await repository.GetByContactIdAsync(contactId);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCount(3);
        results.All(h => h.ContactId == contactId).Should().BeTrue();
        results.Should().BeInAscendingOrder(h => h.Timestamp);
    }    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldReturnMatchingHistories()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new ContactHistoryRepository(context);
        
        // Add some history with the correlation ID
        context.ContactHistories.Add(new ContactHistory(
            contactId, "CREATE", new { id = contactId.ToString(), action = "create" }, correlationId));
        
        context.ContactHistories.Add(new ContactHistory(
            contactId, "UPDATE", new { id = contactId.ToString(), action = "update" }, correlationId));
        
        // Add some history with a different correlation ID
        context.ContactHistories.Add(new ContactHistory(
            contactId, "DELETE", new { id = contactId.ToString(), action = "delete" }, "different-correlation"));
        
        await context.SaveChangesAsync();
        
        // Act
        var results = await repository.GetByCorrelationIdAsync(correlationId);
        
        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCount(2);
        results.All(h => h.CorrelationId == correlationId).Should().BeTrue();
    }
    
    [Fact]
    public async Task GetByOperationTypeAsync_ShouldReturnMatchingHistories()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        // Seed only 3 records so we get exactly 1 UPDATE record (index 1)
        await SeedHistoryRecordsAsync(context, contactId, 3);
        var repository = new ContactHistoryRepository(context);
        
        // Act
        var results = await repository.GetByOperationTypeAsync("UPDATE");
        
        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCount(1);
        results.All(h => h.OperationType == "UPDATE").Should().BeTrue();
    }
    
    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnHistoriesInRange()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        
        // Create 3 records with different timestamps by adding them with delays
        var oldRecord = new ContactHistory(contactId, "CREATE", 
            new { id = contactId.ToString(), data = "old" }, 
            Guid.NewGuid().ToString());
            
        context.ContactHistories.Add(oldRecord);
        await context.SaveChangesAsync();
        
        // Sleep to ensure different timestamps
        await Task.Delay(10);
        
        var inRangeRecord = new ContactHistory(contactId, "UPDATE", 
            new { id = contactId.ToString(), data = "in-range" }, 
            Guid.NewGuid().ToString());
            
        context.ContactHistories.Add(inRangeRecord);
        await context.SaveChangesAsync();
        
        await Task.Delay(10);
        
        var newRecord = new ContactHistory(contactId, "DELETE", 
            new { id = contactId.ToString(), data = "new" }, 
            Guid.NewGuid().ToString());
        
        context.ContactHistories.Add(newRecord);
        await context.SaveChangesAsync();
        
        var repository = new ContactHistoryRepository(context);
        
        // Get all records to check their actual timestamps
        var allRecords = await context.ContactHistories.ToListAsync();
        var minTime = allRecords.Min(r => r.Timestamp);
        var maxTime = allRecords.Max(r => r.Timestamp);
        
        // Use actual timestamp range from the data
        var startDate = minTime.AddMilliseconds(5);
        var endDate = maxTime.AddMilliseconds(-5);
        
        // Act
        var results = await repository.GetByDateRangeAsync(startDate, endDate);
        
        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCount(1); // Should get the middle record
        results.All(h => h.Timestamp >= startDate && h.Timestamp <= endDate).Should().BeTrue();
        results.Should().BeInDescendingOrder(h => h.Timestamp);
    }
    
    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedHistoryRecordsAsync(context, contactId, recordCount: 10);
        var repository = new ContactHistoryRepository(context);
        
        // Act
        var results = await repository.GetAllAsync(skip: 2, take: 3);
        
        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCount(3);
        results.Should().BeInDescendingOrder(h => h.Timestamp);
    }
    
    [Theory]
    [InlineData(-1, 10)]  // Negative skip
    [InlineData(0, 0)]    // Zero take
    [InlineData(0, 1001)] // Take > 1000
    public async Task GetAllAsync_WithInvalidParameters_ShouldThrowArgumentException(int skip, int take)
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new ContactHistoryRepository(context);
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            repository.GetAllAsync(skip, take));
    }
    
    [Fact]
    public async Task GetCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedHistoryRecordsAsync(context, contactId, recordCount: 7);
        var repository = new ContactHistoryRepository(context);
        
        // Act
        var count = await repository.GetCountAsync();
        
        // Assert
        count.Should().Be(7);
    }
    
    [Fact]
    public async Task GetCountByContactIdAsync_ShouldReturnContactHistoryCount()
    {
        // Arrange
        var contactId1 = Guid.NewGuid();
        var contactId2 = Guid.NewGuid();
        
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedHistoryRecordsAsync(context, contactId1, recordCount: 3);
        await SeedHistoryRecordsAsync(context, contactId2, recordCount: 2);
        var repository = new ContactHistoryRepository(context);
        
        // Act
        var count1 = await repository.GetCountByContactIdAsync(contactId1);
        var count2 = await repository.GetCountByContactIdAsync(contactId2);
        
        // Assert
        count1.Should().Be(3);
        count2.Should().Be(2);
    }
    
    [Fact]
    public async Task ExistsAsync_WhenHistoryExists_ShouldReturnTrue()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var contactId = Guid.NewGuid();
        var contactHistory = new ContactHistory(
            contactId, "CREATE", new { id = contactId.ToString(), action = "create" }, "test-correlation");
            
        context.ContactHistories.Add(contactHistory);
        await context.SaveChangesAsync();
        
        var repository = new ContactHistoryRepository(context);
        
        // Act
        var exists = await repository.ExistsAsync(contactHistory.Id);
        
        // Assert
        exists.Should().BeTrue();
    }
    
    [Fact]
    public async Task ExistsAsync_WhenHistoryDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new ContactHistoryRepository(context);
        
        // Act
        var exists = await repository.ExistsAsync(Guid.NewGuid());
        
        // Assert
        exists.Should().BeFalse();
    }
    
    [Fact]
    public async Task GetContactHistoryForReplayAsync_ShouldReturnOrderedHistory()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedHistoryRecordsAsync(context, contactId);
        var repository = new ContactHistoryRepository(context);
        
        var fromTimestamp = DateTime.UtcNow.AddDays(-2);
        
        // Act
        var results = await repository.GetContactHistoryForReplayAsync(contactId, fromTimestamp);
        
        // Assert
        results.Should().NotBeEmpty();
        results.All(h => h.ContactId == contactId && h.Timestamp >= fromTimestamp).Should().BeTrue();
        results.Should().BeInAscendingOrder(h => h.Timestamp);
    }
    
    [Fact]
    public async Task DeleteOldHistoryRecordsAsync_ShouldRemoveOldRecords()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedHistoryRecordsAsync(context, contactId, 3);
        var repository = new ContactHistoryRepository(context);
        
        var olderThan = DateTime.UtcNow.AddDays(-1);
        var expectedDeletions = await context.ContactHistories
            .CountAsync(h => h.Timestamp < olderThan);
        
        // Act
        var deletedCount = await repository.DeleteOldHistoryRecordsAsync(olderThan);
        
        // Assert
        deletedCount.Should().Be(expectedDeletions);
        
        // Verify records were deleted
        var remainingCount = await context.ContactHistories.CountAsync();
        remainingCount.Should().Be(3 - expectedDeletions); // We seeded 3 records
    }
    
    // Helper method to seed history records
    private async Task SeedHistoryRecordsAsync(ContactDbContext context, Guid contactId, int recordCount = 10)
    {
        var baseDate = DateTime.UtcNow.AddDays(-3);
        
        for (int i = 0; i < recordCount; i++)
        {
            string operationType = i % 3 == 0 ? "CREATE" : 
                                 i % 3 == 1 ? "UPDATE" : "DELETE";
            
            context.ContactHistories.Add(new ContactHistory(
                contactId,
                operationType,
                new { id = contactId.ToString(), data = $"test-{i}" },
                Guid.NewGuid().ToString()));
        }
        
        await context.SaveChangesAsync();
    }
}
