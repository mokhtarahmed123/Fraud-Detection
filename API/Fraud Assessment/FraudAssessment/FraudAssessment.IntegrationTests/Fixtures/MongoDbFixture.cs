using FraudAssessment.Infrastructure.Persistence.MongoDB.Context;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace FraudAssessment.IntegrationTests.Fixtures;

public class MongoDbFixture : IAsyncLifetime
{
    private const string DatabaseName = "FraudAssessment_IntegrationTests";

    private readonly MongoClient _client;

    public AssessmentDbContext Context { get; }
    public MongoDbInitializer Initializer { get; }

    public MongoDbFixture()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "MONGODB_TEST_CONNECTION_STRING")
            ?? "mongodb://127.0.0.1:27017/?replicaSet=rs0";

        _client = new MongoClient(connectionString);

        var settings = new Dictionary<string, string?>
        {
            ["MongoDB:ConnectionString"] = connectionString,
            ["MongoDB:DatabaseName"] = DatabaseName
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        Context = new AssessmentDbContext(configuration);
        Initializer = new MongoDbInitializer(Context);
    }

    public async Task InitializeAsync()
    {
        // Start with a clean test database.
        await _client.DropDatabaseAsync(DatabaseName);

        await Initializer.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        // Delete only the dedicated integration-test database.
        await _client.DropDatabaseAsync(DatabaseName);
    }
}