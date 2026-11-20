using FraudAssessment.Application.Features.Assessments.DTOs;
using FraudAssessment.Domain.Enums;
using MediatR;

namespace FraudAssessment.Application.Features.Assessments.Commands.Model
{
    public record CreateAssessmentCommand
    {
        public string ClientId { get; set; } = null!;

        public string AccountId { get; set; } = null!;

        public AssessmentType Type { get; set; }
    }
    public record CreateAssessmentRequest(CreateAssessmentCommand Command, string IdempotencyKey)
        : IRequest<AssessmentResponse>;

}
