using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactService.Infrastructure.Data.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Id)
               .IsRequired()
               .ValueGeneratedNever(); // Domain generates the ID

        builder.Property(e => e.EventType)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.EventData)
               .IsRequired()
               .HasColumnType("TEXT"); // Store JSON as TEXT

        builder.Property(e => e.CorrelationId)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("NOW()");

        builder.Property(e => e.ProcessedAt)
               .IsRequired(false);

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<string>() // Store enum as string
               .HasMaxLength(20);

        builder.Property(e => e.RetryCount)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.ErrorMessage)
               .IsRequired(false)
               .HasMaxLength(1000);

        builder.Property(e => e.NextRetryAt)
               .IsRequired(false);

        // Indexes for performance
        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_OutboxEvents_Status");

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
               .HasDatabaseName("IX_OutboxEvents_Status_CreatedAt");

        builder.HasIndex(e => new { e.Status, e.NextRetryAt })
               .HasDatabaseName("IX_OutboxEvents_Status_NextRetryAt");

        builder.HasIndex(e => e.CorrelationId)
               .HasDatabaseName("IX_OutboxEvents_CorrelationId");

        builder.HasIndex(e => e.EventType)
               .HasDatabaseName("IX_OutboxEvents_EventType");

        // Index for cleanup operations
        builder.HasIndex(e => new { e.Status, e.ProcessedAt })
               .HasDatabaseName("IX_OutboxEvents_Status_ProcessedAt");
    }
}