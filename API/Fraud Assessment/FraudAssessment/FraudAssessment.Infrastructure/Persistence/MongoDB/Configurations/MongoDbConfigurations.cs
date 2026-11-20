using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Configurations
{
    public static class MongoDbConfigurations
    {
        public static void Register()
        {
            BsonSerializer.RegisterSerializer(
             new GuidSerializer(GuidRepresentation.Standard));
            AssessmentConfiguration.Configure();
            IdempotencyRecordConfiguration.Configure();
            OutBoxesMessageConfiguration.Configure();
        }
    }
}
