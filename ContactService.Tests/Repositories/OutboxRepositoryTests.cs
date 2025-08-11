using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Data;
using ContactService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContactService.Tests.Repositories;

// Test helper extension for setting private fields via reflection
public static class OutboxEventTestExtensions
{
    public static OutboxEvent WithCreatedAt(this OutboxEvent outboxEvent, DateTime createdAt)
    {
        typeof(OutboxEvent).GetProperty("CreatedAt")?.SetValue(outboxEvent, createdAt);
        return outboxEvent;
    }
    
    public static OutboxEvent WithStatus(this OutboxEvent outboxEvent, OutboxEventStatus status)
    {
        typeof(OutboxEvent).GetProperty("Status")?.SetValue(outboxEvent, status);
        return outboxEvent;
    }
    
    public static OutboxEvent WithProcessedAt(this OutboxEvent outboxEvent, DateTime? processedAt)
    {
        typeof(OutboxEvent).GetProperty("ProcessedAt")?.SetValue(outboxEvent, processedAt);
        return outboxEvent;
    }
    
    public static OutboxEvent WithRetryCount(this OutboxEvent outboxEvent, int retryCount)
    {
        typeof(OutboxEvent).GetProperty("RetryCount")?.SetValue(outboxEvent, retryCount);
        return outboxEvent;
    }
    
    public static OutboxEvent WithNextRetryAt(this OutboxEvent outboxEvent, DateTime? nextRetryAt)
    {
        typeof(OutboxEvent).GetProperty("NextRetryAt")?.SetValue(outboxEvent, nextRetryAt);
        return outboxEvent;
    }
    
    public static OutboxEvent WithId(this OutboxEvent outboxEvent, Guid id)
    {
        typeof(OutboxEvent).GetProperty("Id")?.SetValue(outboxEvent, id);
        return outboxEvent;
    }
}

public class OutboxRepositoryTests
{
    private readonly DbContextOptions<ContactDbContext> _dbContextOptions;
    
    public OutboxRepositoryTests()
    {
        // Use in-memory database for testing
        _dbContextOptions = new DbContextOptionsBuilder<ContactDbContext>()
            .UseInMemoryDatabase(databaseName: $"ContactDb_Outbox_{Guid.NewGuid()}")
            .Options;
    }
    
    [Fact]
    public async Task CreateAsync_ShouldAddEventToDatabase()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new OutboxRepository(context);
        
        var outboxEvent = new OutboxEvent(
            "contact.created",
            "{\"id\":\"test-id\", \"firstName\":\"John\"}",
            "contacts");
        
        // Act
        var result = await repository.CreateAsync(outboxEvent);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(outboxEvent.Id);
        
        // Verify it was saved to database
        var savedEvent = await context.OutboxEvents
            .FirstOrDefaultAsync(e => e.Id == outboxEvent.Id);
        savedEvent.Should().NotBeNull();
        savedEvent!.EventType.Should().Be("contact.created");
        savedEvent.Status.Should().Be(OutboxEventStatus.Pending);
    }
    
    [Fact]
    public async Task UpdateAsync_ShouldUpdateEventInDatabase()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new OutboxRepository(context);
        
        var outboxEvent = new OutboxEvent(
            "contact.created",
            "{\"id\":\"test-id\", \"firstName\":\"John\"}",
            "contacts");
        
        await context.OutboxEvents.AddAsync(outboxEvent);
        await context.SaveChangesAsync();
        
        // Update the event
        outboxEvent.MarkAsProcessed();
        
        // Act
        var result = await repository.UpdateAsync(outboxEvent);
        
        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OutboxEventStatus.Processed);
        result.ProcessedAt.Should().NotBeNull();
        
        // Verify it was updated in database
        var updatedEvent = await context.OutboxEvents
            .FirstOrDefaultAsync(e => e.Id == outboxEvent.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.Status.Should().Be(OutboxEventStatus.Processed);
        updatedEvent.ProcessedAt.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ShouldReturnEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        using var context = new ContactDbContext(_dbContextOptions);
        
        var outboxEvent = new OutboxEvent(
            "contact.updated",
            new { id = "test-id", firstName = "John" },
            "contacts");
        
        outboxEvent.WithId(eventId);
        
        context.OutboxEvents.Add(outboxEvent);
        await context.SaveChangesAsync();
        
        var repository = new OutboxRepository(context);
        
        // Act
        var result = await repository.GetByIdAsync(eventId);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventId);
        result.EventType.Should().Be("contact.updated");
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        var repository = new OutboxRepository(context);
        
        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetPendingEventsAsync_ShouldReturnPendingEvents()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedOutboxEventsAsync(context);
        var repository = new OutboxRepository(context);
        
        // Act
        var results = await repository.GetPendingEventsAsync();
        
        // Assert
        results.Should().NotBeEmpty();
        results.All(e => e.Status == OutboxEventStatus.Pending).Should().BeTrue();
        results.All(e => e.NextRetryAt == null || e.NextRetryAt <= DateTime.UtcNow).Should().BeTrue();
        results.Should().BeInAscendingOrder(e => e.CreatedAt);
    }
    
    [Fact]
    public async Task GetFailedEventsReadyForRetryAsync_ShouldReturnFailedEventsForRetry()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedOutboxEventsAsync(context);
        var repository = new OutboxRepository(context);
        
        // Act
        var results = await repository.GetFailedEventsReadyForRetryAsync();
        
        // Assert
        results.Should().NotBeEmpty();
        results.All(e => e.Status == OutboxEventStatus.Failed).Should().BeTrue();
        results.All(e => e.RetryCount < 5).Should().BeTrue();
        results.All(e => e.NextRetryAt != null && e.NextRetryAt <= DateTime.UtcNow).Should().BeTrue();
        results.Should().BeInAscendingOrder(e => e.NextRetryAt);
    }
    
    [Fact]
    public async Task DeleteProcessedEventsAsync_ShouldRemoveOldProcessedEvents()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedOutboxEventsAsync(context);
        var repository = new OutboxRepository(context);
        
        var olderThan = DateTime.UtcNow.AddDays(-5);
        
        // Count events that should be deleted
        var expectedDeletionCount = await context.OutboxEvents
            .CountAsync(e => e.Status == OutboxEventStatus.Processed && 
                           e.ProcessedAt != null && 
                           e.ProcessedAt < olderThan);
        
        // Verify we have some events to delete
        expectedDeletionCount.Should().BeGreaterThan(0);
        
        // Remember total count before deletion
        var totalBefore = await context.OutboxEvents.CountAsync();
        
        // Act
        await repository.DeleteProcessedEventsAsync(olderThan);
        
        // Assert
        var totalAfter = await context.OutboxEvents.CountAsync();
        totalAfter.Should().Be(totalBefore - expectedDeletionCount);
        
        // Verify no old processed events remain
        var remainingOldProcessed = await context.OutboxEvents
            .AnyAsync(e => e.Status == OutboxEventStatus.Processed && 
                         e.ProcessedAt != null && 
                         e.ProcessedAt < olderThan);
        remainingOldProcessed.Should().BeFalse();
    }
    
    [Fact]
    public async Task GetPendingEventCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedOutboxEventsAsync(context);
        var repository = new OutboxRepository(context);
        
        var expectedCount = await context.OutboxEvents
            .CountAsync(e => e.Status == OutboxEventStatus.Pending);
        
        // Act
        var result = await repository.GetPendingEventCountAsync();
        
        // Assert
        result.Should().Be(expectedCount);
    }
    
    [Fact]
    public async Task GetFailedEventCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var context = new ContactDbContext(_dbContextOptions);
        await SeedOutboxEventsAsync(context);
        var repository = new OutboxRepository(context);
        
        var expectedCount = await context.OutboxEvents
            .CountAsync(e => e.Status == OutboxEventStatus.Failed);
        
        // Act
        var result = await repository.GetFailedEventCountAsync();
        
        // Assert
        result.Should().Be(expectedCount);
    }
    
    // Helper method to seed outbox events
    private async Task SeedOutboxEventsAsync(ContactDbContext context)
    {
        // Add pending events
        for (int i = 0; i < 5; i++)
        {
            var pendingEvent = new OutboxEvent(
                "contact.created",
                new { id = $"test-{i}", firstName = $"John{i}" },
                "contacts");
            
            pendingEvent
                .WithCreatedAt(DateTime.UtcNow.AddHours(-i));
            
            context.OutboxEvents.Add(pendingEvent);
        }
        
        // Add processed events
        for (int i = 0; i < 5; i++)
        {
            var processedEvent = new OutboxEvent(
                "contact.updated",
                new { id = $"test-{i}", firstName = $"John{i}" },
                "contacts");
            
            processedEvent
                .WithCreatedAt(DateTime.UtcNow.AddDays(-7))
                .WithStatus(OutboxEventStatus.Processed)
                .WithProcessedAt(DateTime.UtcNow.AddDays(-7));
            
            context.OutboxEvents.Add(processedEvent);
        }
        
        // Add recent processed events
        for (int i = 0; i < 2; i++)
        {
            var recentProcessed = new OutboxEvent(
                "contact.updated",
                new { id = $"recent-{i}", firstName = $"Recent{i}" },
                "contacts");
            
            recentProcessed
                .WithCreatedAt(DateTime.UtcNow.AddHours(-1))
                .WithStatus(OutboxEventStatus.Processed)
                .WithProcessedAt(DateTime.UtcNow.AddMinutes(-10));
            
            context.OutboxEvents.Add(recentProcessed);
        }
        
        // Add failed events ready for retry
        for (int i = 0; i < 3; i++)
        {
            var failedEvent = new OutboxEvent(
                "contact.deleted",
                new { id = $"failed-{i}", firstName = $"Failed{i}" },
                "contacts");
            
            failedEvent
                .WithCreatedAt(DateTime.UtcNow.AddHours(-2))
                .WithStatus(OutboxEventStatus.Failed)
                .WithRetryCount(i)
                .WithNextRetryAt(DateTime.UtcNow.AddMinutes(-5));
            
            context.OutboxEvents.Add(failedEvent);
        }
        
        // Add failed events with too many retries
        var maxRetriesEvent = new OutboxEvent(
            "contact.deleted",
            new { id = "max-retries", firstName = "MaxRetries" },
            "contacts");
        
        maxRetriesEvent
            .WithCreatedAt(DateTime.UtcNow.AddHours(-2))
            .WithStatus(OutboxEventStatus.Failed)
            .WithRetryCount(5)
            .WithNextRetryAt(DateTime.UtcNow.AddMinutes(-5));
        
        context.OutboxEvents.Add(maxRetriesEvent);
        
        // Add failed events not yet ready for retry
        var notReadyEvent = new OutboxEvent(
            "contact.deleted",
            new { id = "not-ready", firstName = "NotReady" },
            "contacts");
        
        notReadyEvent
            .WithCreatedAt(DateTime.UtcNow.AddHours(-1))
            .WithStatus(OutboxEventStatus.Failed)
            .WithRetryCount(1)
            .WithNextRetryAt(DateTime.UtcNow.AddHours(1));
        
        context.OutboxEvents.Add(notReadyEvent);
        
        await context.SaveChangesAsync();
    }
}
