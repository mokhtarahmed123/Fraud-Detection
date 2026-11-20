
using FraudAssessment.Application.Interface;
using FraudAssessment.Application.MessagingInterface;
using Microsoft.Extensions.Logging;

namespace FraudAssessment.Infrastructure.Messaging.OutboxMessage;

public class OutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IOutboxRepository outboxRepository,
        IMessageBus messageBus,
        ILogger<OutboxPublisher> logger)
    {
        _outboxRepository = outboxRepository;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task PublishPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        var pendingMessages =
            await _outboxRepository.GetPendingMessagesAsync(
                10,
                cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                // Publish message to the message broker
                await _messageBus.PublishAsync(
                    message.Id,
                    message.EventType,
                    message.Payload,
                    cancellationToken);

                // Mark as published only after successful broker confirmation
                await _outboxRepository.MarkAsPublishedAsync(
                    message.Id,
                    cancellationToken);

                _logger.LogInformation(
                    "Outbox message {MessageId} published successfully. EventType: {EventType}",
                    message.Id,
                    message.EventType);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish Outbox message {MessageId}. EventType: {EventType}",
                    message.Id,
                    message.EventType);

                // Leave the message Pending so it can be retried later.
            }
        }
    }
}
