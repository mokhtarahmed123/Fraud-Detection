namespace FraudAssessment.Domain.DomainEvents;

public record AssessmentReviewedDomainEvent(
    Guid AssessmentId,
    string ReviewReason,
    DateTimeOffset OccurredAt
);