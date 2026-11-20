
using FraudAssessment.Application.Exceptions;
using FraudAssessment.Application.Features.Assessments.Commands.Handler;
using FraudAssessment.Application.Features.Assessments.Commands.Model;
using FraudAssessment.Application.Interface;
using FraudAssessment.Domain.Entities;
using FraudAssessment.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FraudAssessment.UnitTests.Application.Features.Assessments.Commands;

public class CreateAssessmentCommandHandlerTests
{
    private readonly Mock<IAssessmentRepository> _assessmentRepository;
    private readonly Mock<IIdempotencyRepository> _idempotencyRepository;
    private readonly Mock<IAssessmentUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<CreateAssessmentCommandHandler>> _logger;

    private readonly CreateAssessmentCommandHandler _handler;

    public CreateAssessmentCommandHandlerTests()
    {
        _assessmentRepository = new Mock<IAssessmentRepository>();
        _idempotencyRepository = new Mock<IIdempotencyRepository>();
        _unitOfWork = new Mock<IAssessmentUnitOfWork>();
        _logger = new Mock<ILogger<CreateAssessmentCommandHandler>>();

        _handler = new CreateAssessmentCommandHandler(
            _assessmentRepository.Object,
            _idempotencyRepository.Object,
            _unitOfWork.Object, _logger.Object);
    }

    // --------------------------------------------------
    // Normal Assessment Tests
    // --------------------------------------------------

    [Fact]
    public async Task Handle_ShouldCreateAssessment_WhenRequestIsValid()
    {
        // Arrange
        var command = CreateNormalCommand();

        var request = new CreateAssessmentRequest(
            command,
            "test-key");

        _idempotencyRepository
            .Setup(x => x.GetByClientIdAndKeyAsync(
                command.ClientId,
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyRecord?)null);

        _unitOfWork
            .Setup(x => x.SaveNewAssessmentAsync(
                It.IsAny<Assessment>(),
                It.IsAny<IdempotencyRecord>(),
                It.IsAny<OutboxMessage>()
                ,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            request,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.ClientId, result.ClientId);
        Assert.Equal(command.AccountId, result.AccountId);
        Assert.Equal(AssessmentType.Login, result.Type);
        Assert.Equal(AssessmentStatus.Received, result.Status);

        _unitOfWork.Verify(x => x.SaveNewAssessmentAsync(
            It.IsAny<Assessment>(),
            It.IsAny<IdempotencyRecord>(),
            It.IsAny<OutboxMessage>()
                ,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenNormalRequestTypeIsPayment()
    {
        // Arrange
        var command = CreateNormalCommand();
        command.Type = AssessmentType.Payment;

        var request = new CreateAssessmentRequest(
            command,
            "test-key");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(request, CancellationToken.None));

        _idempotencyRepository.Verify(x =>
            x.GetByClientIdAndKeyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(x =>
            x.SaveNewAssessmentAsync(
                It.IsAny<Assessment>(),
                It.IsAny<IdempotencyRecord>(),
                    It.IsAny<OutboxMessage>()
                ,
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --------------------------------------------------
    // Existing Idempotency Tests
    // --------------------------------------------------

    [Fact]
    public async Task Handle_ShouldReturnExistingAssessment_WhenSameKeyAndSameRequest()
    {
        // Arrange
        var command = CreateNormalCommand();

        var request = new CreateAssessmentRequest(
            command,
            "test-key");

        var assessment = Assessment.Create(
            command.ClientId,
            command.AccountId,
            command.Type);

        var requestHash = GenerateRequestHash(
            command.AccountId,
            command.Type);

        var record = IdempotencyRecord.Create(
            "test-key",
            command.ClientId,
            requestHash,
            assessment.Id,
            DateTimeOffset.UtcNow.AddHours(24));

        _idempotencyRepository
            .Setup(x => x.GetByClientIdAndKeyAsync(
                command.ClientId,
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _assessmentRepository
            .Setup(x => x.GetAssessmentByIdAsync(
                assessment.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment);

        // Act
        var result = await _handler.Handle(
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(assessment.Id, result.AssessmentId);
        Assert.Equal(assessment.ClientId, result.ClientId);

        _unitOfWork.Verify(x =>
            x.SaveNewAssessmentAsync(
                It.IsAny<Assessment>(),
                It.IsAny<IdempotencyRecord>(),
                    It.IsAny<OutboxMessage>()
                ,
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowIdempotencyConflictException_WhenSameKeyDifferentRequest()
    {
        // Arrange
        var command = CreateNormalCommand();

        var request = new CreateAssessmentRequest(
            command,
            "test-key");

        var record = IdempotencyRecord.Create(
            "test-key",
            command.ClientId,
            "DIFFERENT-HASH",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(24));

        _idempotencyRepository
            .Setup(x => x.GetByClientIdAndKeyAsync(
                command.ClientId,
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act & Assert
        await Assert.ThrowsAsync<IdempotencyConflictException>(() =>
            _handler.Handle(request, CancellationToken.None));

        _assessmentRepository.Verify(x =>
            x.GetAssessmentByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(x =>
            x.SaveNewAssessmentAsync(
                It.IsAny<Assessment>(),
                It.IsAny<IdempotencyRecord>(),
                It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenOriginalAssessmentDoesNotExist()
    {
        // Arrange
        var command = CreateNormalCommand();

        var request = new CreateAssessmentRequest(
            command,
            "test-key");

        var requestHash = GenerateRequestHash(
            command.AccountId,
            command.Type);

        var record = IdempotencyRecord.Create(
            "test-key",
            command.ClientId,
            requestHash,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(24));

        _idempotencyRepository
            .Setup(x => x.GetByClientIdAndKeyAsync(
                command.ClientId,
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _assessmentRepository
            .Setup(x => x.GetAssessmentByIdAsync(
                record.AssessmentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Assessment?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(request, CancellationToken.None));
    }

    // --------------------------------------------------
    // Payment Assessment Tests
    // --------------------------------------------------

    [Fact]
    public async Task Handle_ShouldCreatePaymentAssessment_WhenRequestIsValid()
    {
        // Arrange
        var command = CreatePaymentCommand();

        var request = new CreatePaymentAssessmentRequest(
            command,
            "payment-key");

        _idempotencyRepository
            .Setup(x => x.GetByClientIdAndKeyAsync(
                command.ClientId,
                "payment-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyRecord?)null);

        _unitOfWork
            .Setup(x => x.SaveNewAssessmentAsync(
                It.IsAny<Assessment>(),
                It.IsAny<IdempotencyRecord>(),
                It.IsAny<OutboxMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            request,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AssessmentType.Payment, result.Type);
        Assert.Equal(command.ClientId, result.ClientId);
        Assert.Equal(command.AccountId, result.AccountId);
        Assert.Equal(AssessmentStatus.Received, result.Status);

        _unitOfWork.Verify(x => x.SaveNewAssessmentAsync(
            It.Is<Assessment>(a =>
                a.Type == AssessmentType.Payment &&
                a.AccountId == command.AccountId),
            It.Is<IdempotencyRecord>(r =>
                r.Key == "payment-key" &&
                r.ClientId == command.ClientId),
            It.IsAny<OutboxMessage>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnExistingPaymentAssessment_WhenSameKeyAndSameRequest()
    {
        // Arrange
        var command = CreatePaymentCommand();

        var request = new CreatePaymentAssessmentRequest(
            command,
            "payment-key");

        var assessment = Assessment.Create(
            command.ClientId,
            command.AccountId,
            AssessmentType.Payment);

        var requestHash = GenerateRequestHash(
            command.AccountId,
            AssessmentType.Payment,
            command.Amount,
            command.Currency);

        var record = IdempotencyRecord.Create(
            "payment-key",
            command.ClientId,
            requestHash,
            assessment.Id,
            DateTimeOffset.UtcNow.AddHours(24));

        _idempotencyRepository
            .Setup(x => x.GetByClientIdAndKeyAsync(
                command.ClientId,
                "payment-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _assessmentRepository
            .Setup(x => x.GetAssessmentByIdAsync(
                assessment.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment);

        // Act
        var result = await _handler.Handle(
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(assessment.Id, result.AssessmentId);
        Assert.Equal(AssessmentType.Payment, result.Type);

        _unitOfWork.Verify(x =>
            x.SaveNewAssessmentAsync(
                It.IsAny<Assessment>(),
                It.IsAny<IdempotencyRecord>(),
                It.IsAny<OutboxMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenPaymentAmountIsDifferent()
    {
        // Arrange
        var command = CreatePaymentCommand();

        var request = new CreatePaymentAssessmentRequest(
            command,
            "payment-key");

        // Hash for an earlier request with a different amount
        var oldHash = GenerateRequestHash(
            command.AccountId,
            AssessmentType.Payment,
            500m,
            command.Currency);

        var record = IdempotencyRecord.Create(
            "payment-key",
            command.ClientId,
            oldHash,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(24));

        _idempotencyRepository
            .Setup(x => x.GetByClientIdAndKeyAsync(
                command.ClientId,
                "payment-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act & Assert
        await Assert.ThrowsAsync<IdempotencyConflictException>(() =>
            _handler.Handle(request, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenPaymentCurrencyIsDifferent()
    {
        // Arrange
        var command = CreatePaymentCommand();

        var request = new CreatePaymentAssessmentRequest(
            command,
            "payment-key");

        // Hash for an earlier request with a different currency
        var oldHash = GenerateRequestHash(
            command.AccountId,
            AssessmentType.Payment,
            command.Amount,
            "EUR");

        var record = IdempotencyRecord.Create(
            "payment-key",
            command.ClientId,
            oldHash,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(24));

        _idempotencyRepository
            .Setup(x => x.GetByClientIdAndKeyAsync(
                command.ClientId,
                "payment-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act & Assert
        await Assert.ThrowsAsync<IdempotencyConflictException>(() =>
            _handler.Handle(request, CancellationToken.None));
    }

    // --------------------------------------------------
    // Helpers
    // --------------------------------------------------

    private static CreateAssessmentCommand CreateNormalCommand()
    {
        return new CreateAssessmentCommand
        {
            ClientId = "client-123",
            AccountId = "account-456",
            Type = AssessmentType.Login
        };
    }

    private static CreatePaymentAssessmentCommand CreatePaymentCommand()
    {
        return new CreatePaymentAssessmentCommand
        {
            ClientId = "client-123",
            AccountId = "account-456",
            Amount = 1000m,
            Currency = "USD"
        };
    }

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
}
