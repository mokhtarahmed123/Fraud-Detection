
using FraudAssessment.Application.Exceptions;
using FraudAssessment.Application.Features.Assessments.DTOs;
using FraudAssessment.Application.Features.Assessments.Queries.Model;
using FraudAssessment.Application.Interface;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FraudAssessment.Application.Features.Assessments.Queries.Handler;

public class GetAssessmentByIdQueryHandler
    : IRequestHandler<GetAssessmentByIdQuery, AssessmentByIdDto>
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly ILogger<GetAssessmentByIdQueryHandler> _logger;
    public GetAssessmentByIdQueryHandler(
        IAssessmentRepository assessmentRepository)
        : this(
            assessmentRepository,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GetAssessmentByIdQueryHandler>.Instance)
    {
    }

    public GetAssessmentByIdQueryHandler(
        IAssessmentRepository assessmentRepository,
        ILogger<GetAssessmentByIdQueryHandler> logger)
    {
        _assessmentRepository = assessmentRepository;
        _logger = logger;
    }

    public async Task<AssessmentByIdDto> Handle(
        GetAssessmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            _logger.LogWarning(
                "Get Assessment By Id request was null.");

            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation(
            "Retrieving Assessment {AssessmentId}.",
            request.Id);

        var assessment = await _assessmentRepository
            .GetAssessmentByIdAsync(
                request.Id,
                cancellationToken);

        if (assessment is null)
        {
            _logger.LogWarning(
                "Assessment {AssessmentId} was not found.",
                request.Id);

            throw new NotFoundException("Assessment not found.");
        }

        _logger.LogInformation(
            "Assessment {AssessmentId} retrieved successfully.",
            request.Id);

        return new AssessmentByIdDto(
            assessment.ClientId,
            assessment.AccountId,
            assessment.Type,
            assessment.Status,
            assessment.CreatedAt,
            assessment.ProcessingStartedAt,
            assessment.CompletedAt,
            assessment.DecisionId
        );
    }
}
