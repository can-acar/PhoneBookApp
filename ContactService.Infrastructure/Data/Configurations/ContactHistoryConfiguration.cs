using ContactService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactService.Infrastructure.Data.Configurations
{
    public class ContactHistoryConfiguration : IEntityTypeConfiguration<ContactHistory>
    {
        public void Configure(EntityTypeBuilder<ContactHistory> builder)
        {
            builder.ToTable("ContactHistory");

            builder.HasKey(ch => ch.Id);

            builder.Property(ch => ch.Id)
                .IsRequired()
                .ValueGeneratedNever(); // We set this in the domain

            builder.Property(ch => ch.ContactId)
                .IsRequired();

            builder.Property(ch => ch.OperationType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(ch => ch.Data)
                .IsRequired()
                .HasColumnType("jsonb"); // PostgreSQL JSONB for better performance

            builder.Property(ch => ch.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("NOW()"); // PostgreSQL function

            builder.Property(ch => ch.CorrelationId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ch => ch.UserId)
                .HasMaxLength(100);

            builder.Property(ch => ch.IPAddress)
                .HasMaxLength(45); // IPv6 can be up to 45 characters

            builder.Property(ch => ch.UserAgent)
                .HasMaxLength(500);

            builder.Property(ch => ch.AdditionalMetadata)
                .HasColumnType("jsonb"); // PostgreSQL JSONB for additional metadata

            // Indexes for better query performance
            builder.HasIndex(ch => ch.ContactId)
                .HasDatabaseName("IX_ContactHistory_ContactId");

            builder.HasIndex(ch => ch.CorrelationId)
                .HasDatabaseName("IX_ContactHistory_CorrelationId");

            builder.HasIndex(ch => ch.OperationType)
                .HasDatabaseName("IX_ContactHistory_OperationType");

            builder.HasIndex(ch => ch.Timestamp)
                .HasDatabaseName("IX_ContactHistory_Timestamp");

            // Composite index for common query patterns
            builder.HasIndex(ch => new { ch.ContactId, ch.Timestamp })
                .HasDatabaseName("IX_ContactHistory_ContactId_Timestamp");

            // Optional foreign key relationship (soft reference)
            // Note: We don't enforce FK constraint since contact might be deleted
            // but we still want to keep the history
            builder.HasIndex(ch => ch.ContactId)
                .HasDatabaseName("IX_ContactHistory_ContactId_Lookup");
        }
    }
}