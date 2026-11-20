
using FraudAssessment.Application.Features.Assessments.DTOs;
using FraudAssessment.Application.Features.Assessments.Queries.Model;
using FraudAssessment.Application.Interface;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FraudAssessment.Application.Features.Assessments.Queries.Handler;

public class GetAllByClientIdQueryHandler
    : IRequestHandler<GetAllByClientIdQuery, IEnumerable<AssessmentDto>>
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly ILogger<GetAllByClientIdQueryHandler> _logger;

    public GetAllByClientIdQueryHandler(
        IAssessmentRepository assessmentRepository)
        : this(
            assessmentRepository,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GetAllByClientIdQueryHandler>.Instance)
    {
    }
    public GetAllByClientIdQueryHandler(
        IAssessmentRepository assessmentRepository,
        ILogger<GetAllByClientIdQueryHandler> logger)
    {
        _assessmentRepository = assessmentRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<AssessmentDto>> Handle(
        GetAllByClientIdQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.clientId))
        {
            _logger.LogWarning(
                "Get assessments request received with an empty ClientId.");

            throw new ArgumentNullException(
                "Client ID is required.",
                nameof(request.clientId));
        }

        _logger.LogInformation(
            "Retrieving assessments for Client {ClientId}.",
            request.clientId);

        var assessments = await _assessmentRepository
            .GetAssessmentsByClientIdAsync(
                request.clientId,
                cancellationToken);

        if (assessments.Count == 0)
        {
            _logger.LogInformation(
                "No assessments found for Client {ClientId}.",
                request.clientId);

            return Enumerable.Empty<AssessmentDto>();
        }

        _logger.LogInformation(
            "Retrieved {AssessmentCount} assessments for Client {ClientId}.",
            assessments.Count,
            request.clientId);

        return assessments.Select(a => new AssessmentDto(
            a.Id,
            a.AccountId,
            a.Type,
            a.Status,
            a.CreatedAt,
            a.CompletedAt,
            a.DecisionId
        ));
    }
}
