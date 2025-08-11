using ContactService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactService.Infrastructure.Data.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever(); // Domain generates the ID

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Company)
            .HasMaxLength(200);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        // Configure relationships
        builder.HasMany(c => c.ContactInfos)
            .WithOne(ci => ci.Contact)
            .HasForeignKey(ci => ci.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.FirstName, c.LastName })
            .HasDatabaseName("IX_Contacts_FullName");

        builder.HasIndex(c => c.Company)
            .HasDatabaseName("IX_Contacts_Company");
    }
}