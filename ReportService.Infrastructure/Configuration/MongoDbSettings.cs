namespace ReportService.Infrastructure.Configuration;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ReportsCollectionName { get; set; } = "Reports";
    public string LocationStatisticsCollectionName { get; set; } = "LocationStatistics";
}