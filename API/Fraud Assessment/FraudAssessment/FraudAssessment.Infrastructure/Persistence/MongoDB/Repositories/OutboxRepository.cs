using DnsClient.Internal;
using FraudAssessment.Application.Interface;
using FraudAssessment.Domain.Entities;
using FraudAssessment.Domain.Enums;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Context;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly AssessmentDbContext assessmentDbContext;
        private readonly ILogger<OutboxRepository> logger;

        public OutboxRepository(AssessmentDbContext assessmentDbContext, ILogger<OutboxRepository> logger)
        {
            this.assessmentDbContext = assessmentDbContext;
            this.logger = logger;
        }
        public async Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            await assessmentDbContext.Outbox.InsertOneAsync(
            outboxMessage,
       cancellationToken: cancellationToken);

            logger.LogInformation(
                "Outbox message {OutboxMessageId} added for Assessment {AssessmentId}",
                outboxMessage.Id,
                outboxMessage.AssessmentId);

        }

        public async Task<IReadOnlyCollection<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken)
        {
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize));

            var messages = await assessmentDbContext.Outbox
                .Find(x => x.Status == OutboxMessageStatus.Pending)
                .SortBy(x => x.OccurredAt)
                .Limit(batchSize)
                .ToListAsync(cancellationToken);

            return messages;
        }

        public async Task MarkAsPublishedAsync(Guid outboxMessageId, CancellationToken cancellationToken)
        {
            var update = Builders<OutboxMessage>.Update
                .Set(x => x.Status, OutboxMessageStatus.Published)
                .Set(x => x.PublishedAt, DateTimeOffset.UtcNow);

            var result = await assessmentDbContext.Outbox.UpdateOneAsync(
                x => x.Id == outboxMessageId,
                update,
                cancellationToken: cancellationToken);

            if (result.MatchedCount == 0)
            {
                logger.LogWarning(
                    "Outbox message {OutboxMessageId} was not found",
                    outboxMessageId);

                throw new InvalidOperationException(
                    $"Outbox message {outboxMessageId} was not found.");
            }

            logger.LogInformation(
                "Outbox message {OutboxMessageId} marked as published",
                outboxMessageId);
        }
    }
}
