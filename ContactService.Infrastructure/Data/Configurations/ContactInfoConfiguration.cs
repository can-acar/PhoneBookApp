using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactService.Infrastructure.Data.Configurations;

public class ContactInfoConfiguration : IEntityTypeConfiguration<ContactInfo>
{
    public void Configure(EntityTypeBuilder<ContactInfo> builder)
    {
        builder.ToTable("ContactInfos");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Id)
            .ValueGeneratedNever(); // Domain generates the ID

        builder.Property(ci => ci.ContactId)
            .IsRequired();

        builder.Property(ci => ci.InfoType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ci => ci.Content)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ci => ci.CreatedAt)
            .IsRequired();

        builder.HasOne(ci => ci.Contact)
            .WithMany(c => c.ContactInfos)
            .HasForeignKey(ci => ci.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ci => ci.ContactId)
            .HasDatabaseName("IX_ContactInfos_ContactId");

        builder.HasIndex(ci => ci.InfoType)
            .HasDatabaseName("IX_ContactInfos_InfoType");

        builder.HasIndex(ci => new { ci.InfoType, ci.Content })
            .HasDatabaseName("IX_ContactInfos_Type_Content");
    }
}