using FraudAssessment.Domain.Entities;

namespace FraudAssessment.Application.Interface
{
    public interface IAssessmentUnitOfWork
    {
        Task SaveNewAssessmentAsync(
            Assessment assessment,
            IdempotencyRecord idempotencyRecord, OutboxMessage outboxMessage,
            CancellationToken cancellationToken = default);
    }
}
