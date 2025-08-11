using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly MongoDbContext _context;

        public NotificationRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Notification notification)
        {
            // No need to modify Id or CreatedAt as they are set in the entity constructor
            await _context.Notifications.InsertOneAsync(notification);
        }

        public async Task<Notification> GetByIdAsync(Guid id)
        {
            return await _context.Notifications.Find(n => n.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId)
        {
            return await _context.Notifications.Find(n => n.UserId == userId)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUndeliveredAsync()
        {
            return await _context.Notifications.Find(n => !n.IsDelivered && n.SentAt == null)
                .SortBy(n => n.Priority)
                .ToListAsync();
        }

        public async Task UpdateAsync(Notification notification)
        {
            await _context.Notifications.ReplaceOneAsync(
                n => n.Id == notification.Id, 
                notification, 
                new ReplaceOptions { IsUpsert = false });
        }

        public async Task<IEnumerable<Notification>> GetByCorrelationIdAsync(string correlationId)
        {
            return await _context.Notifications.Find(n => n.CorrelationId == correlationId)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
}
