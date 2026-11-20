using FraudAssessment.Domain.Enums;

namespace FraudAssessment.UnitTests.Domain.Entities;

public class AssessmentTests
{
    [Fact]
    public void CreateAssessment_ShouldReturnAssessment_WhenValidParameters()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;

        // Act
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);

        // Assert
        Assert.NotNull(assessment);
        Assert.Equal(clientId, assessment.ClientId);
        Assert.Equal(accountId, assessment.AccountId);
        Assert.Equal(type, assessment.Type);
        Assert.Equal(AssessmentStatus.Received, assessment.Status);
        Assert.NotEqual(Guid.Empty, assessment.Id);
    }

    [Fact]
    public void CreateAssessment_ShouldThrowArgumentException_WhenClientIdIsNullOrEmpty()
    {
        // Arrange
        string clientId = null!;
        var accountId = "account456";
        var type = AssessmentType.Login;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            FraudAssessment.Domain.Entities.Assessment.Create(
                clientId, accountId, type));
    }

    [Fact]
    public void CreateAssessment_ShouldThrowArgumentException_WhenAccountIdIsNullOrEmpty()
    {
        // Arrange
        var clientId = "client123";
        string accountId = null!;
        var type = AssessmentType.AccountChange;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            FraudAssessment.Domain.Entities.Assessment.Create(
                clientId, accountId, type));
    }

    [Fact]
    public void CreateAssessment_ShouldThrowArgumentException_WhenTypeIsNotDefined()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        AssessmentType type = (AssessmentType)(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            FraudAssessment.Domain.Entities.Assessment.Create(
                clientId, accountId, type));
    }

    [Fact]
    public void StartProcessing_ShouldUpdateStatusAndProcessingStartedAt_WhenStatusIsReceived()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        // Act
        assessment.StartProcessing();
        // Assert
        Assert.Equal(AssessmentStatus.Processing, assessment.Status);
        Assert.NotNull(assessment.ProcessingStartedAt);
    }
    [Fact]
    public void StartProcessing_ShouldThrowInvalidOperationException_WhenStatusIsNotReceived()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        assessment.StartProcessing(); // Change status to Processing
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assessment.StartProcessing());
    }
    [Fact]
    public void CompleteAssessment_ShouldUpdateStatusAndCompletedAt_WhenStatusIsProcessing()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var decisionId = new Guid("12345678-1234-1234-1234-1234567890ab");
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        assessment.StartProcessing(); // Change status to Processing
        // Act
        assessment.Complete(decisionId);
        // Assert
        Assert.Equal(AssessmentStatus.Completed, assessment.Status);
        Assert.NotNull(assessment.CompletedAt);
        Assert.Equal(decisionId, assessment.DecisionId);

    }

    [Fact]
    public void CompleteAssessment_ShouldThrowInvalidOperationException_WhenStatusIsReceived()
    {
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var decisionId = new Guid("12345678-1234-1234-1234-1234567890ab");
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        Assert.Throws<InvalidOperationException>(() => assessment.Complete(decisionId));

    }
    [Fact]
    public void CompleteAssessment_ShouldThrowInvalidOperationException_WhenStatusCompleted()
    {
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var decisionId = new Guid("12345678-1234-1234-1234-1234567890ab");
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        assessment.StartProcessing();
        assessment.Complete(decisionId);
        Assert.Throws<InvalidOperationException>(() => assessment.Complete(decisionId));



    }
    [Fact]
    public void CompleteAssessment_ShouldThrowInvalidOperationException_WhenStatusFail()
    {
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var decisionId = new Guid("12345678-1234-1234-1234-1234567890ab");
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);

        assessment.StartProcessing();
        assessment.Fail("Fail");
        Assert.Throws<InvalidOperationException>(() => assessment.Complete(decisionId));



    }
    [Fact]
    public void CompleteAssessment_ShouldThrowArgumentException_WhenDecisionIdIsEmpty()
    {
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var decisionId = new Guid();
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        assessment.StartProcessing();

        Assert.Throws<ArgumentException>(() => assessment.Complete(decisionId));



    }

    [Fact]
    public void FailAssessment_ShouldUpdateStatus_WhenStatusIsProcessing()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        assessment.StartProcessing(); // Change status to Processing
        // Act
        assessment.Fail("Fail");
        // Assert
        Assert.Equal(AssessmentStatus.Failed, assessment.Status);

    }
    [Fact]
    public void FailAssessment_ShouldThrowInvalidOperationException_WhenStatusIsReceived()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        // Act
        Assert.Throws<InvalidOperationException>(() => assessment.Fail("Fail"));

    }
    [Fact]
    public void FailAssessment_ShouldThrowInvalidOperationException_WhenStatusIsCompleted()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        assessment.StartProcessing(); // Change status to Processing
        assessment.Complete(new Guid("12345678-1234-1234-1234-1234567890ab")); // Change status to Completed
        // Act
        Assert.Throws<InvalidOperationException>(() => assessment.Fail("Fail"));
    }
    [Fact]
    public void FailAssessment_ShouldThrowInvalidOperationException_WhenStatusIsFailed()
    {
        // Arrange
        var clientId = "client123";
        var accountId = "account456";
        var type = AssessmentType.Payment;
        var assessment = FraudAssessment.Domain.Entities.Assessment.Create(
            clientId, accountId, type);
        assessment.StartProcessing(); // Change status to Processing
        assessment.Fail("Fail");
        // Change status to Failed
        // Act
        Assert.Throws<InvalidOperationException>(() => assessment.Fail("Fail"));
    }
}
