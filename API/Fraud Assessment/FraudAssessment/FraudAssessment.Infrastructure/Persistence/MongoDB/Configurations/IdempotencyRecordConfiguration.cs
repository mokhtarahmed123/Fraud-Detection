using FraudAssessment.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Configurations;

public static class IdempotencyRecordConfiguration
{
    public static void Configure()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(IdempotencyRecord)))
        {
            BsonClassMap.RegisterClassMap<IdempotencyRecord>(classMap =>
            {
                classMap.AutoMap();
            });
        }
    }

}