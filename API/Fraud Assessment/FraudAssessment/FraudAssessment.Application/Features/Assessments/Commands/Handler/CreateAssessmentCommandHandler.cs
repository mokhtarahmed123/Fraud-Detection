
using FraudAssessment.Application.Exceptions;
using FraudAssessment.Application.Features.Assessments.Commands.Model;
using FraudAssessment.Application.Features.Assessments.DTOs;
using FraudAssessment.Application.Interface;
using FraudAssessment.Application.Models;
using FraudAssessment.Domain.Entities;
using FraudAssessment.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FraudAssessment.Application.Features.Assessments.Commands.Handler;

public class CreateAssessmentCommandHandler :
    IRequestHandler<CreateAssessmentRequest, AssessmentResponse>,
    IRequestHandler<CreatePaymentAssessmentRequest, AssessmentResponse>
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IAssessmentUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAssessmentCommandHandler> _logger;

    public CreateAssessmentCommandHandler(
        IAssessmentRepository assessmentRepository,
        IIdempotencyRepository idempotencyRepository,
        IAssessmentUnitOfWork unitOfWork,
        ILogger<CreateAssessmentCommandHandler> logger)
    {
        _assessmentRepository = assessmentRepository;
        _idempotencyRepository = idempotencyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // Normal Assessment
    public Task<AssessmentResponse> Handle(
        CreateAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Command.Type == AssessmentType.Payment)
        {
            _logger.LogWarning(
                "Payment assessment was submitted through the normal assessment endpoint.");

            throw new InvalidOperationException(
                "Use CreatePaymentAssessmentRequest for Payment assessments.");
        }

        return CreateOrGetAssessmentAsync(
            request.Command.ClientId,
            request.Command.AccountId,
            request.Command.Type,
            request.IdempotencyKey,
            cancellationToken);
    }

    // Payment Assessment
    public Task<AssessmentResponse> Handle(
        CreatePaymentAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        return CreateOrGetAssessmentAsync(
            request.Command.ClientId,
            request.Command.AccountId,
            AssessmentType.Payment,
            request.IdempotencyKey,
            cancellationToken,
            request.Command.Amount,
            request.Command.Currency);
    }

    // Shared Workflow
    private async Task<AssessmentResponse> CreateOrGetAssessmentAsync(
        string clientId,
        string accountId,
        AssessmentType type,
        string idempotencyKey,
        CancellationToken cancellationToken,
        decimal? amount = null,
        string? currency = null)
    {
        _logger.LogInformation(
            "Assessment request received for Client {ClientId}, Type {AssessmentType}.",
            clientId,
            type);

        var requestHash = GenerateRequestHash(
            accountId,
            type,
            amount,
            currency);

        // Check Idempotency
        var existingRecord =
            await _idempotencyRepository.GetByClientIdAndKeyAsync(
                clientId,
                idempotencyKey,
                cancellationToken);

        if (existingRecord is not null)
        {
            if (existingRecord.RequestHash != requestHash)
            {
                _logger.LogWarning(
                    "Idempotency conflict detected for Client {ClientId}.",
                    clientId);

                throw new IdempotencyConflictException();
            }

            var existingAssessment =
                await _assessmentRepository.GetAssessmentByIdAsync(
                    existingRecord.AssessmentId,
                    cancellationToken);

            if (existingAssessment is null)
            {
                _logger.LogError(
                    "Assessment {AssessmentId} referenced by an Idempotency record was not found.",
                    existingRecord.AssessmentId);

                throw new InvalidOperationException(
                    "The original assessment could not be found.");
            }

            _logger.LogInformation(
                "Idempotency replay detected. Returning existing Assessment {AssessmentId}.",
                existingAssessment.Id);

            return MapToResponse(existingAssessment);
        }

        // Create Assessment
        var assessment = Assessment.Create(
            clientId,
            accountId,
            type);

        // Create Idempotency Record
        var idempotencyRecord = IdempotencyRecord.Create(
            idempotencyKey,
            clientId,
            requestHash,
            assessment.Id,
            DateTimeOffset.UtcNow.AddHours(24));

        // Create Outbox Message

        var processAssessmentMessage = new ProcessAssessmentMessage(
              assessment.Id,
                assessment.Type);

        var outboxPayload = JsonSerializer.Serialize(processAssessmentMessage);

        var outboxMessage = OutboxMessage.Create(
            assessment.Id,
            "ProcessAssessment",
            outboxPayload,
            DateTimeOffset.UtcNow);


        _logger.LogInformation(
            "Creating new Assessment {AssessmentId} for Client {ClientId}.",
            assessment.Id,
            clientId);

        // Save Assessment + Idempotency + Outbox in one transaction
        await _unitOfWork.SaveNewAssessmentAsync(
            assessment,
            idempotencyRecord,
            outboxMessage,
            cancellationToken);

        _logger.LogInformation(
            "Assessment {AssessmentId} created successfully with Outbox message {OutboxMessageId}.",
            assessment.Id,
            outboxMessage.Id);

        return MapToResponse(assessment);
    }

    // Generate Request Hash
    private static string GenerateRequestHash(
        string accountId,
        AssessmentType type,
        decimal? amount = null,
        string? currency = null)
    {
        var requestData =
            $"{accountId}|{type}|{amount?.ToString(CultureInfo.InvariantCulture)}|{currency?.Trim().ToUpperInvariant()}";

        var hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(requestData));

        return Convert.ToHexString(hashBytes);
    }

    // Mapping
    private static AssessmentResponse MapToResponse(
        Assessment assessment)
    {
        return new AssessmentResponse(
            assessment.Id,
            assessment.ClientId,
            assessment.AccountId,
            assessment.Type,
            assessment.Status,
            assessment.CreatedAt);
    }
}
