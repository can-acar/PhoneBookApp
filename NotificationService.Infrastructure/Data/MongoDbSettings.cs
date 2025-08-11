namespace NotificationService.Infrastructure.Data;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string NotificationsCollectionName { get; set; } = "Notifications";
    public string TemplatesCollectionName { get; set; } = "NotificationTemplates";
}