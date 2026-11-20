
using FraudAssessment.Application.Interface;
using FraudAssessment.Domain.Entities;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Context;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FraudAssessment.Infrastructure.Persistence.MongoDB.Repositories;

public class AssessmentUnitOfWork : IAssessmentUnitOfWork
{
    private readonly IMongoClient _mongoClient;
    private readonly AssessmentDbContext _context;
    private readonly ILogger<AssessmentUnitOfWork> _logger;

    public AssessmentUnitOfWork(
        IMongoClient mongoClient,
        AssessmentDbContext context,
        ILogger<AssessmentUnitOfWork> logger)
    {
        _mongoClient = mongoClient;
        _context = context;
        _logger = logger;
    }

    public async Task SaveNewAssessmentAsync(
        Assessment assessment,
        IdempotencyRecord idempotencyRecord, OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(
            cancellationToken: cancellationToken);

        session.StartTransaction();

        try
        {
            await _context.Assessments.InsertOneAsync(
                session,
                assessment,
                cancellationToken: cancellationToken);

            await _context.IdempotencyRecords.InsertOneAsync(
                session,
                idempotencyRecord,
                cancellationToken: cancellationToken);

            await _context.Outbox.InsertOneAsync(
                session,
             outboxMessage,
              cancellationToken: cancellationToken
                );

            await session.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Assessment {AssessmentId} and Idempotency record saved successfully in one transaction.",
                assessment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Transaction failed while saving Assessment {AssessmentId}.",
                assessment.Id);

            if (session.IsInTransaction)
            {
                try
                {
                    await session.AbortTransactionAsync(cancellationToken);
                }
                catch (Exception abortException)
                {
                    _logger.LogError(
                        abortException,
                        "Failed to abort transaction for Assessment {AssessmentId}.",
                        assessment.Id);
                }
            }

            throw;
        }
    }
}
