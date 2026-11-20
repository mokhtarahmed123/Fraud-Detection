using FraudAssessment.Domain.Common;

namespace FraudAssessment.Domain.Entities;

public class IdempotencyRecord : Entity
{
    public string Key { get; private set; } = null!;
    public string ClientId { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public Guid AssessmentId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private IdempotencyRecord() { }

    public static IdempotencyRecord Create(
        string key,
        string clientId,
        string requestHash,
        Guid assessmentId,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(key));

        if (key.Length > 200)
            throw new ArgumentException(
                "Idempotency key cannot exceed 200 characters.",
                nameof(key));

        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException(
                "Client ID is required.",
                nameof(clientId));

        if (string.IsNullOrWhiteSpace(requestHash))
            throw new ArgumentException(
                "Request hash is required.",
                nameof(requestHash));

        if (assessmentId == Guid.Empty)
            throw new ArgumentException(
                "Assessment ID is required.",
                nameof(assessmentId));

        var createdAt = DateTimeOffset.UtcNow;

        if (expiresAt <= createdAt)
            throw new ArgumentException(
                "Expiration date must be in the future.",
                nameof(expiresAt));

        return new IdempotencyRecord
        {
            Key = key,
            ClientId = clientId,
            RequestHash = requestHash,
            AssessmentId = assessmentId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };
    }
}