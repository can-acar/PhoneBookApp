using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories
{
    public class NotificationTemplateRepository : INotificationTemplateRepository
    {
        private readonly MongoDbContext _context;

        public NotificationTemplateRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(NotificationTemplate template)
        {
            template.Id = template.Id == Guid.Empty ? Guid.NewGuid() : template.Id;
            template.CreatedAt = DateTime.UtcNow;
            
            await _context.NotificationTemplates.InsertOneAsync(template);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _context.NotificationTemplates.DeleteOneAsync(t => t.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<IEnumerable<NotificationTemplate>> GetAllActiveAsync()
        {
            return await _context.NotificationTemplates.Find(t => t.IsActive)
                .ToListAsync();
        }

        public async Task<NotificationTemplate?> GetByNameAsync(string templateName, string? language = null)
        {
            var filter = Builders<NotificationTemplate>.Filter.And(
                Builders<NotificationTemplate>.Filter.Eq(t => t.Name, templateName),
                Builders<NotificationTemplate>.Filter.Eq(t => t.IsActive, true));

            if (!string.IsNullOrEmpty(language))
            {
                filter = Builders<NotificationTemplate>.Filter.And(
                    filter,
                    Builders<NotificationTemplate>.Filter.Eq(t => t.Language, language));
            }

            return await _context.NotificationTemplates.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(NotificationTemplate template)
        {
            template.UpdatedAt = DateTime.UtcNow;
            
            await _context.NotificationTemplates.ReplaceOneAsync(
                t => t.Id == template.Id, 
                template, 
                new ReplaceOptions { IsUpsert = false });
        }
    }
}
