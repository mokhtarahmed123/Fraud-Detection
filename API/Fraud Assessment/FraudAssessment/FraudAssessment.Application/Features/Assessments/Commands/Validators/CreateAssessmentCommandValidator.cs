using FluentValidation;
using FraudAssessment.Application.Features.Assessments.Commands.Model;

namespace FraudAssessment.Application.Features.Assessments.Commands.Validators
{
    public class CreateAssessmentCommandValidator
        : AbstractValidator<CreateAssessmentRequest>
    {
        public CreateAssessmentCommandValidator()
        {
            RuleFor(x => x.Command.ClientId)
                .NotEmpty()
                .WithMessage("Client ID is required.");

            RuleFor(x => x.Command.AccountId)
                .NotEmpty()
                .WithMessage("Account ID is required.");

            RuleFor(x => x.Command.Type)
                .IsInEnum()
                .WithMessage("Invalid assessment type.");

            RuleFor(x => x.IdempotencyKey)
                .NotEmpty()
                .WithMessage("Idempotency key is required.")
                .MaximumLength(200)
                .WithMessage("Idempotency key cannot exceed 200 characters.");
        }
    }
}
