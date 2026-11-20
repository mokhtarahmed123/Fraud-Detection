namespace FraudAssessment.Domain.DomainEvents
{
    public record AssessmentProcessingStartedDomainEvent
        (
            Guid AssessmentId,
            DateTimeOffset OccurredAt
        );
}
