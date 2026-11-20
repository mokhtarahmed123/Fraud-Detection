
using FraudAssessment.Application.Interface;
using FraudAssessment.Domain.Entities;
using FraudAssessment.Domain.Enums;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Context;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Repositories
{
    public class AssessmentRepository : IAssessmentRepository
    {
        private readonly AssessmentDbContext _assessmentDbContext;
        private readonly ILogger<AssessmentRepository> _logger;

        public AssessmentRepository(
            AssessmentDbContext assessmentDbContext,
            ILogger<AssessmentRepository> logger)
        {
            _assessmentDbContext = assessmentDbContext;
            _logger = logger;
        }

        public async Task AddAssessmentAsync(
            Assessment assessment,
            CancellationToken cancellationToken)
        {
            try
            {
                await _assessmentDbContext.Assessments.InsertOneAsync(
                    assessment,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Assessment {AssessmentId} created successfully for Client {ClientId}.",
                    assessment.Id,
                    assessment.ClientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create Assessment {AssessmentId} for Client {ClientId}.",
                    assessment.Id,
                    assessment.ClientId);

                throw;
            }
        }

        public async Task<Assessment?> GetAssessmentByIdAsync(
            Guid assessmentId,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _assessmentDbContext.Assessments
                    .Find(a => a.Id == assessmentId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve Assessment {AssessmentId}.",
                    assessmentId);

                throw;
            }
        }

        public async Task<IReadOnlyList<Assessment>> GetAssessmentsByClientIdAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _assessmentDbContext.Assessments
                    .Find(a => a.ClientId == clientId)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve Assessments for Client {ClientId}.",
                    clientId);

                throw;
            }
        }

        public async Task<bool> TryUpdateReceivedAssessmentAsync(Guid assessmentId, Assessment assessment, CancellationToken cancellationToken)
        {
            var filter = Builders<Assessment>.Filter.And(
              Builders<Assessment>.Filter.Eq(x => x.Id, assessmentId),
              Builders<Assessment>.Filter.Eq(
            x => x.Status,
         AssessmentStatus.Received));

            var result = await _assessmentDbContext.Assessments
                .ReplaceOneAsync(
                    filter,
                    assessment,
                    cancellationToken: cancellationToken);

            return result.MatchedCount == 1;
        }

        public async Task UpdateAssessmentAsync(
            Guid assessmentId,
            Assessment assessment,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _assessmentDbContext.Assessments.ReplaceOneAsync(
                    a => a.Id == assessmentId,
                    assessment,
                    cancellationToken: cancellationToken);

                if (result.MatchedCount == 0)
                {
                    _logger.LogWarning(
                        "Assessment {AssessmentId} was not found during update.",
                        assessmentId);

                    throw new KeyNotFoundException(
                        $"Assessment with ID {assessmentId} was not found.");
                }

                _logger.LogInformation(
                    "Assessment {AssessmentId} updated successfully.",
                    assessmentId);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update Assessment {AssessmentId}.",
                    assessmentId);

                throw;
            }
        }
    }
}
