using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ReportService.Domain.Entities;
using ReportService.Infrastructure.Configuration;
using ReportService.Infrastructure.Mapping;

namespace ReportService.Infrastructure.Data;

public class ReportMongoContext : IReportMongoContext
{
    private readonly IMongoDatabase _database;

    public ReportMongoContext(IOptions<MongoDbSettings> settings)
    {
        // Register MongoDB mappings for domain entities
        ReportEntityMapping.RegisterMappings();

        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<Report> Reports => 
        _database.GetCollection<Report>("Reports");

    public IMongoCollection<LocationStatistic> LocationStatistics => 
        _database.GetCollection<LocationStatistic>("LocationStatistics");
}