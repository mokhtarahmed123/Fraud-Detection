using FraudAssessment.Domain.Enums;

namespace FraudAssessment.Application.Features.Assessments.DTOs;

public record AssessmentByIdDto(

    string ClientId,
    string AccountId,
    AssessmentType Type,
    AssessmentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? CompletedAt,
    Guid? DecisionId
);