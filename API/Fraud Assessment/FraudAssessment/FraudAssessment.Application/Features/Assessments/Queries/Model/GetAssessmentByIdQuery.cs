using FraudAssessment.Application.Features.Assessments.DTOs;
using MediatR;

namespace FraudAssessment.Application.Features.Assessments.Queries.Model
{
    public record GetAssessmentByIdQuery(Guid Id) : IRequest<AssessmentByIdDto>
 ;
}
