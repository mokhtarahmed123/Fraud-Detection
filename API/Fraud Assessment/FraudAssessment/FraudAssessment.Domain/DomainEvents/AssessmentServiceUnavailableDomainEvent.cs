namespace FraudAssessment.Domain.DomainEvents
{
    public record AssessmentServiceUnavailableDomainEvent

    (
        Guid AssessmentId,
        string ServiceName,
        DateTimeOffset OccurredAt
    );
}
