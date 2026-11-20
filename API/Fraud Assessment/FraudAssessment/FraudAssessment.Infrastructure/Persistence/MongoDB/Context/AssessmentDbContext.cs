
using FraudAssessment.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Context;

public class AssessmentDbContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<AssessmentDbContext> _logger;

    public AssessmentDbContext(IConfiguration configuration)
        : this(
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AssessmentDbContext>.Instance)
    {
    }

    public AssessmentDbContext(
        IConfiguration configuration,
        ILogger<AssessmentDbContext> logger)
    {
        _logger = logger;

        var connectionString = configuration["MongoDB:ConnectionString"]
            ?? throw new InvalidOperationException(
                "MongoDB connection string is missing.");

        var databaseName = configuration["MongoDB:DatabaseName"]
            ?? throw new InvalidOperationException(
                "MongoDB database name is missing.");

        try
        {
            var client = new MongoClient(connectionString);

            _database = client.GetDatabase(databaseName);

            _logger.LogInformation(
                "MongoDB database context initialized for database {DatabaseName}",
                databaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initialize MongoDB database context for {DatabaseName}",
                databaseName);

            throw;
        }
    }

    public IMongoCollection<Assessment> Assessments =>
        _database.GetCollection<Assessment>("Assessments");

    public IMongoCollection<IdempotencyRecord> IdempotencyRecords =>
        _database.GetCollection<IdempotencyRecord>("IdempotencyRecords");

    public IMongoCollection<OutboxMessage> Outbox =>
        _database.GetCollection<OutboxMessage>("Outbox");
}
