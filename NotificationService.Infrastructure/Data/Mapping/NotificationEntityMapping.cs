using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Data.Mapping;

public static class NotificationEntityMapping
{
    public static void RegisterMappings()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Notification)))
        {
            BsonClassMap.RegisterClassMap<Notification>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
                
                // Map Id with BsonId attribute
                map.MapIdMember(n => n.Id)
                   .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.GuidGenerator.Instance)
                   .SetSerializer(new GuidSerializer(BsonType.String));

                // Map enums to string
                map.MapMember(n => n.Priority)
                   .SetSerializer(new EnumSerializer<NotificationPriority>(BsonType.String));
                
                map.MapMember(n => n.PreferredProvider)
                   .SetSerializer(new EnumSerializer<ProviderType>(BsonType.String));

                // Map dictionary properly
                map.MapMember(n => n.AdditionalData)
                   .SetElementName("additionalData");

                // Don't serialize computed properties
                map.UnmapProperty(n => n.HasRecipientEmail);
                map.UnmapProperty(n => n.HasRecipientPhoneNumber);
                map.UnmapProperty(n => n.HasBeenSent);
                map.UnmapProperty(n => n.IsHighPriority);
                map.UnmapProperty(n => n.HasFailed);
                map.UnmapProperty(n => n.AdditionalDataReadOnly);
            });
        }
    }
}