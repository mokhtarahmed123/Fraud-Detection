using FraudAssessment.Domain.Enums;

namespace FraudAssessment.Application.Features.Assessments.DTOs
{
    public record AssessmentResponse(
       Guid AssessmentId,
       string ClientId,
       string AccountId,
       AssessmentType Type,
       AssessmentStatus Status,
       DateTimeOffset CreatedAt
   );

}
