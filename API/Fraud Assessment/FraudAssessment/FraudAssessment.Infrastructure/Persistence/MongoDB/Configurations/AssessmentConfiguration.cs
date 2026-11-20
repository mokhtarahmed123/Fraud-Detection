using FraudAssessment.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Configurations;

public static class AssessmentConfiguration
{
    public static void Configure()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Assessment)))
            return;

        BsonClassMap.RegisterClassMap<Assessment>(classMap =>
        {

            classMap.AutoMap();
        });
    }
}