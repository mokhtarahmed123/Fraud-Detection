using FraudAssessment.Domain.Enums;
namespace FraudAssessment.Domain.Entities;
public class OutboxMessage
{
    public Guid Id { get; private set; }

    public Guid AssessmentId { get; private set; }

    public string EventType { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public int RetryCount { get; private set; }

    public OutboxMessageStatus Status { get; private set; }

    public DateTimeOffset? NextRetryAt { get; private set; }

    private OutboxMessage() { }
    private OutboxMessage(Guid assessmentId, string eventType, string payload, DateTimeOffset occurredAt)
    {
        Id = Guid.NewGuid();
        AssessmentId = assessmentId;
        EventType = eventType;
        Payload = payload;
        OccurredAt = occurredAt;
        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
    }

    public static OutboxMessage Create(Guid assessmentId, string eventType, string payload, DateTimeOffset occurredAt)
    {
        if (assessmentId == Guid.Empty)
            throw new ArgumentException("AssessmentId is required.");
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required.");
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.");

        return new OutboxMessage(assessmentId, eventType, payload, occurredAt);
    }

}
