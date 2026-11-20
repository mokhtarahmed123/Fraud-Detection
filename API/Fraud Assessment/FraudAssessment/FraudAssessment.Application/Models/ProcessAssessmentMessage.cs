using FraudAssessment.Domain.Enums;

namespace FraudAssessment.Application.Models
{
    public record ProcessAssessmentMessage(
        Guid AssessmentId,
        AssessmentType Type
    );
}
