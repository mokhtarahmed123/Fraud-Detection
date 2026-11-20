
using FraudAssessment.Infrastructure.Persistence.MongoDB.Configurations;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Context;

public class MongoDbInitializer
{
    private readonly AssessmentDbContext _context;
    private readonly ILogger<MongoDbInitializer> _logger;

    public MongoDbInitializer(AssessmentDbContext context)
        : this(
            context,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MongoDbInitializer>.Instance)
    {
    }
    public MongoDbInitializer(
        AssessmentDbContext context,
        ILogger<MongoDbInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MongoDB initialization started.");

        try
        {
            MongoDbConfigurations.Register();

            _logger.LogDebug("MongoDB configurations registered.");

            await CreateAssessmentIndexesAsync(cancellationToken);
            await CreateIdempotencyIndexesAsync(cancellationToken);
            await CreateOutboxMessageIndexesAsync(cancellationToken);


            _logger.LogInformation(
                "MongoDB initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDB initialization failed.");

            throw;
        }
    }

    private async Task CreateAssessmentIndexesAsync(
        CancellationToken cancellationToken)
    {
        var indexKeys = Builders<Domain.Entities.Assessment>.IndexKeys
            .Ascending(x => x.ClientId)
            .Descending(x => x.CreatedAt);

        var indexModel =
            new CreateIndexModel<Domain.Entities.Assessment>(
                indexKeys,
                new CreateIndexOptions
                {
                    Name = "IX_ClientId_CreatedAt"
                });

        await _context.Assessments.Indexes.CreateOneAsync(
            indexModel,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Assessment index {IndexName} created successfully.",
            "IX_ClientId_CreatedAt");
    }

    private async Task CreateIdempotencyIndexesAsync(
        CancellationToken cancellationToken)
    {
        var indexKeys = Builders<Domain.Entities.IdempotencyRecord>.IndexKeys
            .Ascending(x => x.ClientId)
            .Ascending(x => x.Key);

        var indexModel =
            new CreateIndexModel<Domain.Entities.IdempotencyRecord>(
                indexKeys,
                new CreateIndexOptions
                {
                    Name = "UX_ClientId_Key",
                    Unique = true
                });

        await _context.IdempotencyRecords.Indexes.CreateOneAsync(
            indexModel,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Idempotency unique index {IndexName} created successfully.",
            "UX_ClientId_Key");
    }

    private async Task CreateOutboxMessageIndexesAsync(
        CancellationToken cancellationToken)
    {
        var indexKeys = Builders<Domain.Entities.OutboxMessage>.IndexKeys
            .Ascending(x => x.Status)
            .Ascending(x => x.OccurredAt);
        var indexModel =
            new CreateIndexModel<Domain.Entities.OutboxMessage>(
                indexKeys,
                new CreateIndexOptions
                {
                    Name = "IX_Status_OccurredAt"
                });
        await _context.Outbox.Indexes.CreateOneAsync(
            indexModel,
            cancellationToken: cancellationToken);
        _logger.LogInformation(
            "OutboxMessage index {IndexName} created successfully.",
            "IX_Status_OccurredAt");
    }
}
