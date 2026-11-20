
using FraudAssessment.API.Controllers;
using FraudAssessment.Application.Features.Assessments.Commands.Model;
using FraudAssessment.Application.Features.Assessments.DTOs;
using FraudAssessment.Application.Features.Assessments.Queries.Model;
using FraudAssessment.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FraudAssessment.UnitTests.Controllers;

public class AssessmentsControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly AssessmentsController _controller;

    public AssessmentsControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _controller = new AssessmentsController(_senderMock.Object);
    }

    [Fact]
    public async Task Add_ShouldReturn201Created()
    {
        // Arrange
        var command = new CreateAssessmentCommand
        {
            ClientId = "Client-1",
            AccountId = "Account-1",
            Type = AssessmentType.AccountChange
        };

        var idempotencyKey = "key-123";

        var expectedResponse = new AssessmentResponse(
            Guid.NewGuid(),
            command.ClientId,
            command.AccountId,
            command.Type,
            AssessmentStatus.Received,
            DateTimeOffset.UtcNow
        );

        _senderMock
            .Setup(x => x.Send(
                It.IsAny<CreateAssessmentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Add(
            command,
            idempotencyKey,
            CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(expectedResponse, createdResult.Value);

        _senderMock.Verify(x => x.Send(
            It.Is<CreateAssessmentRequest>(r =>
                r.Command == command &&
                r.IdempotencyKey == idempotencyKey),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPayment_ShouldReturn201Created()
    {
        // Arrange
        var command = new CreatePaymentAssessmentCommand
        {
            ClientId = "Client-1",
            AccountId = "Account-1",
            Amount = 1500,
            Currency = "EGP"
        };

        var idempotencyKey = "payment-key-123";

        var expectedResponse = new AssessmentResponse(
            Guid.NewGuid(),
            command.ClientId,
            command.AccountId,
            command.Type,
            AssessmentStatus.Received,
            DateTimeOffset.UtcNow
        );

        _senderMock
            .Setup(x => x.Send(
                It.IsAny<CreatePaymentAssessmentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.AddPayment(
            command,
            idempotencyKey,
            CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(expectedResponse, createdResult.Value);

        _senderMock.Verify(x => x.Send(
            It.Is<CreatePaymentAssessmentRequest>(r =>
                r.Command == command &&
                r.IdempotencyKey == idempotencyKey),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllByClientId_ShouldReturn200OK()
    {
        // Arrange
        var clientId = "Client-1";

        var expectedResponse = new List<AssessmentDto>
        {
            new(
                Guid.NewGuid(),
                "Account-1",
                AssessmentType.Payment,
                AssessmentStatus.Completed,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                Guid.NewGuid()
            ),
            new(
                Guid.NewGuid(),
                "Account-2",
                AssessmentType.AccountChange,
                AssessmentStatus.Received,
                DateTimeOffset.UtcNow,
                null,
                null
            )
        };

        _senderMock
            .Setup(x => x.Send(
                It.IsAny<GetAllByClientIdQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse.AsEnumerable());

        // Act
        var result = await _controller.GetAllByClientId(
            clientId,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedResponse, okResult.Value);

        _senderMock.Verify(x => x.Send(
            It.Is<GetAllByClientIdQuery>(q =>
                q.clientId == clientId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturn200OK()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();

        var expectedResponse = new AssessmentByIdDto(
            "Client-1",
            "Account-1",
            AssessmentType.Payment,
            AssessmentStatus.Completed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        _senderMock
            .Setup(x => x.Send(
                It.IsAny<GetAssessmentByIdQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetById(
            assessmentId,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedResponse, okResult.Value);

        _senderMock.Verify(x => x.Send(
            It.Is<GetAssessmentByIdQuery>(q =>
                q.Id == assessmentId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
