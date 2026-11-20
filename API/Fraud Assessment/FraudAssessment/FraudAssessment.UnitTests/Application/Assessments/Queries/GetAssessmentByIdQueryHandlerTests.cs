
using FraudAssessment.Application.Exceptions;
using FraudAssessment.Application.Features.Assessments.Queries.Handler;
using FraudAssessment.Application.Features.Assessments.Queries.Model;
using FraudAssessment.Application.Interface;
using FraudAssessment.Domain.Entities;
using FraudAssessment.Domain.Enums;
using Moq;

namespace FraudAssessment.UnitTests.Application.Features.Assessments.Queries;

public class GetAssessmentByIdQueryHandlerTests
{
    private readonly Mock<IAssessmentRepository> _assessmentRepository;
    private readonly GetAssessmentByIdQueryHandler _handler;

    public GetAssessmentByIdQueryHandlerTests()
    {
        _assessmentRepository = new Mock<IAssessmentRepository>();

        _handler = new GetAssessmentByIdQueryHandler(
            _assessmentRepository.Object);
    }

    // --------------------------------------------------
    // Get Assessment Tests
    // --------------------------------------------------

    [Fact]
    public async Task Handle_ShouldReturnAssessment_WhenAssessmentExists()
    {
        // Arrange
        var assessment = CreateAssessmentWithStatus(
            AssessmentStatus.Completed);

        var query = new GetAssessmentByIdQuery(assessment.Id);

        _assessmentRepository
            .Setup(x => x.GetAssessmentByIdAsync(
                assessment.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment);

        // Act
        var result = await _handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(assessment.ClientId, result.ClientId);
        Assert.Equal(assessment.AccountId, result.AccountId);
        Assert.Equal(assessment.Type, result.Type);
        Assert.Equal(assessment.Status, result.Status);
        Assert.Equal(assessment.CreatedAt, result.CreatedAt);
        Assert.Equal(
            assessment.ProcessingStartedAt,
            result.ProcessingStartedAt);
        Assert.Equal(assessment.CompletedAt, result.CompletedAt);
        Assert.Equal(assessment.DecisionId, result.DecisionId);

        _assessmentRepository.Verify(x =>
            x.GetAssessmentByIdAsync(
                assessment.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --------------------------------------------------
    // Assessment Status Tests
    // --------------------------------------------------

    [Theory]
    [InlineData(AssessmentStatus.Received)]
    [InlineData(AssessmentStatus.Processing)]
    [InlineData(AssessmentStatus.Completed)]
    [InlineData(AssessmentStatus.Failed)]
    public async Task Handle_ShouldReturnCorrectData_ForEachAssessmentStatus(
        AssessmentStatus expectedStatus)
    {
        // Arrange
        var assessment = CreateAssessmentWithStatus(expectedStatus);

        var query = new GetAssessmentByIdQuery(assessment.Id);

        _assessmentRepository
            .Setup(x => x.GetAssessmentByIdAsync(
                assessment.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment);

        // Act
        var result = await _handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        Assert.Equal(expectedStatus, result.Status);

        Assert.Equal(assessment.ClientId, result.ClientId);
        Assert.Equal(assessment.AccountId, result.AccountId);
        Assert.Equal(assessment.Type, result.Type);
        Assert.Equal(assessment.CreatedAt, result.CreatedAt);

        Assert.Equal(
            assessment.ProcessingStartedAt,
            result.ProcessingStartedAt);

        Assert.Equal(
            assessment.CompletedAt,
            result.CompletedAt);

        Assert.Equal(
            assessment.DecisionId,
            result.DecisionId);
    }

    // --------------------------------------------------
    // Not Found Test
    // --------------------------------------------------

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenAssessmentDoesNotExist()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();

        var query = new GetAssessmentByIdQuery(assessmentId);

        _assessmentRepository
            .Setup(x => x.GetAssessmentByIdAsync(
                assessmentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Assessment?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None));

        Assert.Equal("Assessment not found.", exception.Message);

        _assessmentRepository.Verify(x =>
            x.GetAssessmentByIdAsync(
                assessmentId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --------------------------------------------------
    // Null Request Test
    // --------------------------------------------------

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetAssessmentByIdQuery request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _handler.Handle(request, CancellationToken.None));

        _assessmentRepository.Verify(x =>
            x.GetAssessmentByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --------------------------------------------------
    // Cancellation Token Test
    // --------------------------------------------------

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        var query = new GetAssessmentByIdQuery(assessmentId);

        _assessmentRepository
            .Setup(x => x.GetAssessmentByIdAsync(
                assessmentId,
                cancellationToken))
            .ReturnsAsync((Assessment?)null);

        // Act
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(query, cancellationToken));

        // Assert
        _assessmentRepository.Verify(x =>
            x.GetAssessmentByIdAsync(
                assessmentId,
                cancellationToken),
            Times.Once);
    }

    // --------------------------------------------------
    // Helper
    // --------------------------------------------------

    private static Assessment CreateAssessmentWithStatus(
        AssessmentStatus status)
    {
        var assessment = Assessment.Create(
            "client-123",
            "account-456",
            AssessmentType.Payment);

        var decisionId = Guid.NewGuid();

        switch (status)
        {
            case AssessmentStatus.Received:
                break;

            case AssessmentStatus.Processing:
                assessment.StartProcessing();
                break;

            case AssessmentStatus.Completed:
                assessment.StartProcessing();
                assessment.Complete(decisionId);
                break;

            case AssessmentStatus.Failed:
                assessment.StartProcessing();
                assessment.Fail("Fail");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return assessment;
    }
}
