using ContactService.Domain.Entities;
using ContactService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ContactService.Infrastructure.Data;

public class ContactDbContext : DbContext
{
    public ContactDbContext(DbContextOptions<ContactDbContext> options) : base(options)
    {
    }

    public DbSet<Contact?> Contacts { get; set; }
    public DbSet<ContactInfo> ContactInfos { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<ContactHistory> ContactHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ContactConfiguration());
        modelBuilder.ApplyConfiguration(new ContactInfoConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxEventConfiguration());
        modelBuilder.ApplyConfiguration(new ContactHistoryConfiguration());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // EF Core will handle the private setters automatically
        // We don't need to manually set these values since they're set in the domain model
        return await base.SaveChangesAsync(cancellationToken);
    }
}