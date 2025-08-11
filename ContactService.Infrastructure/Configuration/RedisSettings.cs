namespace ContactService.Infrastructure.Configuration;

public class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int Database { get; set; } = 0;
    public string InstanceName { get; set; } = "ContactService";
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan ContactCacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan LocationStatsCacheExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public bool Enabled { get; set; } = true;
}