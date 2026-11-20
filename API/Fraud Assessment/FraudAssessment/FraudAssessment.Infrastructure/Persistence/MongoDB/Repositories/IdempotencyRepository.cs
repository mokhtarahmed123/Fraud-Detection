
using FraudAssessment.Application.Interface;
using FraudAssessment.Domain.Entities;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Context;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Repositories
{
    public class IdempotencyRepository : IIdempotencyRepository
    {
        private readonly AssessmentDbContext _assessmentDbContext;
        private readonly ILogger<IdempotencyRepository> _logger;

        public IdempotencyRepository(
            AssessmentDbContext assessmentDbContext,
            ILogger<IdempotencyRepository> logger)
        {
            _assessmentDbContext = assessmentDbContext;
            _logger = logger;
        }

        public async Task AddAsync(
            IdempotencyRecord record,
            CancellationToken cancellationToken)
        {
            try
            {
                await _assessmentDbContext.IdempotencyRecords.InsertOneAsync(
                    record,
                    cancellationToken: cancellationToken);

                _logger.LogDebug(
                    "Idempotency record created for Client {ClientId}, Assessment {AssessmentId}.",
                    record.ClientId,
                    record.AssessmentId);
            }
            catch (MongoWriteException ex)
                when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger.LogWarning(
                    ex,
                    "Duplicate idempotency key detected for Client {ClientId}.",
                    record.ClientId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create Idempotency record for Client {ClientId}.",
                    record.ClientId);

                throw;
            }
        }

        public async Task DeleteExpiredAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;

                var filter = Builders<IdempotencyRecord>.Filter
                    .Lt(record => record.ExpiresAt, now);

                var result = await _assessmentDbContext.IdempotencyRecords
                    .DeleteManyAsync(filter, cancellationToken);

                if (result.DeletedCount > 0)
                {
                    _logger.LogInformation(
                        "Deleted {DeletedCount} expired Idempotency records.",
                        result.DeletedCount);
                }
                else
                {
                    _logger.LogDebug(
                        "No expired Idempotency records found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete expired Idempotency records.");

                throw;
            }
        }

        public async Task<IdempotencyRecord?> GetByClientIdAndKeyAsync(
            string clientId,
            string key,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _assessmentDbContext.IdempotencyRecords
                    .Find(record =>
                        record.ClientId == clientId &&
                        record.Key == key)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve Idempotency record for Client {ClientId}.",
                    clientId);

                throw;
            }
        }
    }
}
