
using FraudAssessment.Domain.Entities;

namespace FraudAssessment.UnitTests.Domain.Entities;

public class IdempotencyRecordTests
{
    [Fact]
    public void Create_ShouldReturnIdempotencyRecord_WhenValidParameters()
    {
        // Arrange
        var key = "test-idempotency-key";
        var clientId = "client-123";
        var requestHash = "ABC123HASH";
        var assessmentId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        // Act
        var record = IdempotencyRecord.Create(
            key,
            clientId,
            requestHash,
            assessmentId,
            expiresAt);

        // Assert
        Assert.NotNull(record);
        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(key, record.Key);
        Assert.Equal(clientId, record.ClientId);
        Assert.Equal(requestHash, record.RequestHash);
        Assert.Equal(assessmentId, record.AssessmentId);
        Assert.True(record.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(expiresAt, record.ExpiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrowArgumentException_WhenKeyIsNullOrWhiteSpace(
        string? key)
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            IdempotencyRecord.Create(
                key!,
                "client-123",
                "ABC123HASH",
                Guid.NewGuid(),
                expiresAt));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenKeyExceeds200Characters()
    {
        // Arrange
        var key = new string('a', 201);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            IdempotencyRecord.Create(
                key,
                "client-123",
                "ABC123HASH",
                Guid.NewGuid(),
                expiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrowArgumentException_WhenClientIdIsNullOrWhiteSpace(
        string? clientId)
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            IdempotencyRecord.Create(
                "test-key",
                clientId!,
                "ABC123HASH",
                Guid.NewGuid(),
                expiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrowArgumentException_WhenRequestHashIsNullOrWhiteSpace(
        string? requestHash)
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            IdempotencyRecord.Create(
                "test-key",
                "client-123",
                requestHash!,
                Guid.NewGuid(),
                expiresAt));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenAssessmentIdIsEmpty()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            IdempotencyRecord.Create(
                "test-key",
                "client-123",
                "ABC123HASH",
                Guid.Empty,
                expiresAt));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenExpiresAtIsInThePast()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            IdempotencyRecord.Create(
                "test-key",
                "client-123",
                "ABC123HASH",
                Guid.NewGuid(),
                expiresAt));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenExpiresAtIsNow()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            IdempotencyRecord.Create(
                "test-key",
                "client-123",
                "ABC123HASH",
                Guid.NewGuid(),
                expiresAt));
    }
}
