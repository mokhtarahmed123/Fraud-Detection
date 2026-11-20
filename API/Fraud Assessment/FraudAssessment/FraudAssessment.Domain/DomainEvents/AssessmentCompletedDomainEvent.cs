namespace FraudAssessment.Domain.DomainEvents
{
    public record AssessmentCompletedDomainEvent(
    Guid AssessmentId,
    Guid? DecisionId,
    DateTimeOffset OccurredAt
);
}
