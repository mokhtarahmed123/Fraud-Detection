using FraudAssessment.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Configurations
{
    public static class OutBoxesMessageConfiguration
    {
        public static void Configure()
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(OutboxMessage)))
                return;

            BsonClassMap.RegisterClassMap<OutboxMessage>(classMap =>
            {

                classMap.AutoMap();
            });
        }
    }
}
