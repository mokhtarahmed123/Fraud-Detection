using FraudAssessment.Application.Features.Assessments.DTOs;
using MediatR;

namespace FraudAssessment.Application.Features.Assessments.Queries.Model
{
    public record GetAllByClientIdQuery(string clientId) : IRequest<IEnumerable<AssessmentDto>>
    {
    }
}
