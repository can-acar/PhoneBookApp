using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;

namespace ReportService.Infrastructure.Mapping;

public static class ReportEntityMapping
{
    public static void RegisterMappings()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Report)))
        {
            BsonClassMap.RegisterClassMap<Report>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
                
                // Map Id with BsonId attribute
                map.MapIdMember(r => r.Id)
                   .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.GuidGenerator.Instance)
                   .SetSerializer(new GuidSerializer(BsonType.String));

                // Map enum to string
                map.MapMember(r => r.Status)
                   .SetSerializer(new EnumSerializer<ReportStatus>(BsonType.String));

                // Map collections
                map.MapMember(r => r.LocationStatistics)
                   .SetElementName("locationStatistics");

                // Map computed properties as read-only (don't serialize)
                map.MapProperty(r => r.ProcessingDuration).SetIgnoreIfNull(true);
                map.MapProperty(r => r.IsCompleted).SetIgnoreIfNull(true);
                map.MapProperty(r => r.HasFailed).SetIgnoreIfNull(true);
                map.MapProperty(r => r.IsInProgress).SetIgnoreIfNull(true);
                map.MapProperty(r => r.LocationStatisticsReadOnly).SetIgnoreIfNull(true);
                
                // Don't serialize computed properties
                map.UnmapProperty(r => r.ProcessingDuration);
                map.UnmapProperty(r => r.IsCompleted);
                map.UnmapProperty(r => r.HasFailed);
                map.UnmapProperty(r => r.IsInProgress);
                map.UnmapProperty(r => r.LocationStatisticsReadOnly);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(LocationStatistic)))
        {
            BsonClassMap.RegisterClassMap<LocationStatistic>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
                
                // Map Id with BsonId attribute for nested documents
                map.MapIdMember(ls => ls.Id)
                   .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.GuidGenerator.Instance)
                   .SetSerializer(new GuidSerializer(BsonType.String));

                // Don't serialize computed properties
                map.UnmapProperty(ls => ls.AveragePhoneNumbersPerPerson);
                map.UnmapProperty(ls => ls.HasMultiplePhoneNumbers);
                
                // Don't serialize navigation property to avoid circular reference
                map.UnmapProperty(ls => ls.Report);
            });
        }
    }
}