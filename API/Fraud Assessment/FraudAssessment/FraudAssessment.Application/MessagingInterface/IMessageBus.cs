namespace FraudAssessment.Application.MessagingInterface
{
    public interface IMessageBus
    {
        Task PublishAsync(
            Guid MessageId,
        string eventType,
        string payload,
        CancellationToken cancellationToken);
    }
}
