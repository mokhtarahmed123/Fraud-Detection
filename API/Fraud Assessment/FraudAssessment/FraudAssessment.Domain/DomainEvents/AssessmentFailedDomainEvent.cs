namespace FraudAssessment.Domain.DomainEvents;

public record AssessmentFailedDomainEvent(
    Guid AssessmentId,
    string FailureReason,
    DateTimeOffset OccurredAt
);