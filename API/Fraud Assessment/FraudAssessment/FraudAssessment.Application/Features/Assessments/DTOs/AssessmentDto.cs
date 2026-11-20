using FraudAssessment.Domain.Enums;

namespace FraudAssessment.Application.Features.Assessments.DTOs
{
    public record AssessmentDto(
    Guid AssessmentId,
    string AccountId,
    AssessmentType Type,
    AssessmentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Guid? DecisionId
);
}
