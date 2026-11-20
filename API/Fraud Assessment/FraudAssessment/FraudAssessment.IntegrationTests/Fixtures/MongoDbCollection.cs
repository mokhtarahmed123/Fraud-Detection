namespace FraudAssessment.IntegrationTests.Fixtures;

[CollectionDefinition("MongoDB Integration Tests",
    DisableParallelization = true)]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>
{
}