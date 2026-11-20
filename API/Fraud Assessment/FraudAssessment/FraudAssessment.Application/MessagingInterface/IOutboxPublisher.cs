namespace FraudAssessment.Application.MessagingInterface
{
    public interface IOutboxPublisher
    {
        Task PublishPendingMessagesAsync(
        CancellationToken cancellationToken);
    }
}
