
using FraudAssessment.Application.Features.Assessments.Queries.Handler;
using FraudAssessment.Application.Features.Assessments.Queries.Model;
using FraudAssessment.Application.Interface;
using FraudAssessment.Domain.Entities;
using FraudAssessment.Domain.Enums;
using Moq;

namespace FraudAssessment.UnitTests.Application.Features.Assessments.Queries;

public class GetAllByClientIdQueryHandlerTests
{
    private readonly Mock<IAssessmentRepository> _assessmentRepository;
    private readonly GetAllByClientIdQueryHandler _handler;

    public GetAllByClientIdQueryHandlerTests()
    {
        _assessmentRepository = new Mock<IAssessmentRepository>();

        _handler = new GetAllByClientIdQueryHandler(
            _assessmentRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAssessments_WhenAssessmentsExist()
    {
        // Arrange
        var clientId = "client-123";

        var assessments = new List<Assessment>
        {
            Assessment.Create(
                clientId,
                "account-101",
                AssessmentType.Payment),

            Assessment.Create(
                clientId,
                "account-102",
                AssessmentType.Login)
        };

        var query = new GetAllByClientIdQuery(clientId);

        _assessmentRepository
            .Setup(x => x.GetAssessmentsByClientIdAsync(
                clientId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessments);

        // Act
        var result = (await _handler.Handle(
            query,
            CancellationToken.None)).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal("account-101", result[0].AccountId);
        Assert.Equal(AssessmentType.Payment, result[0].Type);

        Assert.Equal("account-102", result[1].AccountId);
        Assert.Equal(AssessmentType.Login, result[1].Type);

        Assert.All(result, item =>
            Assert.Equal(AssessmentStatus.Received, item.Status));

        _assessmentRepository.Verify(x =>
            x.GetAssessmentsByClientIdAsync(
                clientId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoAssessmentsExist()
    {
        // Arrange
        var clientId = "client-123";

        var query = new GetAllByClientIdQuery(clientId);

        _assessmentRepository
            .Setup(x => x.GetAssessmentsByClientIdAsync(
                clientId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assessment>());

        // Act
        var result = await _handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenClientIdIsNull()
    {
        // Arrange
        var query = new GetAllByClientIdQuery(null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
           _handler.Handle(query, CancellationToken.None));

        //Assert.Equal("clientId", exception.ParamName);

        _assessmentRepository.Verify(x =>
            x.GetAssessmentsByClientIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var clientId = "client-123";
        var cancellationToken = new CancellationToken();

        var query = new GetAllByClientIdQuery(clientId);

        _assessmentRepository
            .Setup(x => x.GetAssessmentsByClientIdAsync(
                clientId,
                cancellationToken))
            .ReturnsAsync(new List<Assessment>());

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _assessmentRepository.Verify(x =>
            x.GetAssessmentsByClientIdAsync(
                clientId,
                cancellationToken),
            Times.Once);
    }
}
