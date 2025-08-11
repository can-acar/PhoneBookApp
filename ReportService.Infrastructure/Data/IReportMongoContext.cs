using MongoDB.Driver;
using ReportService.Domain.Entities;

namespace ReportService.Infrastructure.Data;

public interface IReportMongoContext
{
    IMongoCollection<Report> Reports { get; }
    IMongoCollection<LocationStatistic> LocationStatistics { get; }
}