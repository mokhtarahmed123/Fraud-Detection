using FraudAssessment.Application.Features.Assessments.DTOs;
using FraudAssessment.Domain.Enums;
using MediatR;

namespace FraudAssessment.Application.Features.Assessments.Commands.Model
{
    public class CreatePaymentAssessmentCommand
    {
        public string ClientId { get; set; } = null!;

        public string AccountId { get; set; } = null!;


        public AssessmentType Type { get; set; } = AssessmentType.Payment;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;





    }
    public record CreatePaymentAssessmentRequest(CreatePaymentAssessmentCommand Command, string IdempotencyKey) :
        IRequest<AssessmentResponse>;

}
