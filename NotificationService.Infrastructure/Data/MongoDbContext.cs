using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Data.Mapping;

namespace NotificationService.Infrastructure.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> options)
        {
            // Register MongoDB mappings for domain entities
            NotificationEntityMapping.RegisterMappings();

            var client = new MongoClient(options.Value.ConnectionString);
            _database = client.GetDatabase(options.Value.DatabaseName);
        }

        public IMongoCollection<Notification> Notifications => 
            _database.GetCollection<Notification>(nameof(Notifications));

        public IMongoCollection<NotificationTemplate> NotificationTemplates => 
            _database.GetCollection<NotificationTemplate>(nameof(NotificationTemplates));

        public async Task CreateIndexesAsync()
        {
            // Notification collection indexes
            var notificationIndexCursor = await Notifications.Indexes.ListAsync();
            var notificationIndexNames = new List<string>();

            // Use ToList() to get all items from cursor
            var notificationIndexes = notificationIndexCursor.ToList();
            foreach (var index in notificationIndexes)
            {
                notificationIndexNames.Add(index["name"].AsString);
            }

            if (!notificationIndexNames.Contains("UserId_1"))
            {
                await Notifications.Indexes.CreateOneAsync(
                    new CreateIndexModel<Notification>(
                        Builders<Notification>.IndexKeys.Ascending(n => n.UserId),
                        new CreateIndexOptions { Name = "UserId_1" }));
            }

            if (!notificationIndexNames.Contains("CorrelationId_1"))
            {
                await Notifications.Indexes.CreateOneAsync(
                    new CreateIndexModel<Notification>(
                        Builders<Notification>.IndexKeys.Ascending(n => n.CorrelationId),
                        new CreateIndexOptions { Name = "CorrelationId_1" }));
            }

            if (!notificationIndexNames.Contains("IsDelivered_1"))
            {
                await Notifications.Indexes.CreateOneAsync(
                    new CreateIndexModel<Notification>(
                        Builders<Notification>.IndexKeys.Ascending(n => n.IsDelivered),
                        new CreateIndexOptions { Name = "IsDelivered_1" }));
            }

            // Template collection indexes
            var templateIndexCursor = await NotificationTemplates.Indexes.ListAsync();
            var templateIndexNames = new List<string>();

            var templateIndexes = templateIndexCursor.ToList();
            foreach (var index in templateIndexes)
            {
                templateIndexNames.Add(index["name"].AsString);
            }

            if (!templateIndexNames.Contains("Name_1_Language_1"))
            {
                await NotificationTemplates.Indexes.CreateOneAsync(
                    new CreateIndexModel<NotificationTemplate>(
                        Builders<NotificationTemplate>.IndexKeys.Ascending(t => t.Name).Ascending(t => t.Language),
                        new CreateIndexOptions { Name = "Name_1_Language_1" }));
            }

            if (!templateIndexNames.Contains("IsActive_1"))
            {
                await NotificationTemplates.Indexes.CreateOneAsync(
                    new CreateIndexModel<NotificationTemplate>(
                        Builders<NotificationTemplate>.IndexKeys.Ascending(t => t.IsActive),
                        new CreateIndexOptions { Name = "IsActive_1" }));
            }
        }
    }
}
