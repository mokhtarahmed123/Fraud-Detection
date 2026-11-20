using FraudAssessment.Domain.Entities;

namespace FraudAssessment.Application.Interface;

public interface IOutboxRepository
{
    Task AddAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task MarkAsPublishedAsync(
        Guid outboxMessageId,
        CancellationToken cancellationToken);
}