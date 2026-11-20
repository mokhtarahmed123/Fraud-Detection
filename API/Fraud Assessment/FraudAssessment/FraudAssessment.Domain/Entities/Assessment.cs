using FraudAssessment.Domain.Common;
using FraudAssessment.Domain.DomainEvents;
using FraudAssessment.Domain.Enums;

namespace FraudAssessment.Domain.Entities
{
    public class Assessment : Entity
    {
        public string ClientId { get; private set; } = null!;

        public string AccountId { get; private set; } = null!;

        public AssessmentType Type { get; private set; }

        public AssessmentStatus Status { get; private set; }
        public DateTimeOffset? ProcessingStartedAt { get; private set; }

        public string? FailureReason { get; private set; }

        public Guid? DecisionId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? CompletedAt { get; private set; }


        private Assessment()
        {
        }

        private Assessment(
            string clientId,
            string accountId,
            AssessmentType type)
        {
            ClientId = clientId;
            AccountId = accountId;
            Type = type;
            Status = AssessmentStatus.Received;
            CreatedAt = DateTimeOffset.UtcNow;

        }

        public static Assessment Create(
            string clientId,
            string accountId,
            AssessmentType type)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("ClientId is required.");

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("SubjectId is required.");

            if (!Enum.IsDefined(type))
                throw new ArgumentException("Invalid assessment type.");

            return new Assessment(clientId, accountId, type);
        }

        public void StartProcessing()
        {
            if (Status != AssessmentStatus.Received)
                throw new InvalidOperationException(
                    "Assessment must be received before processing.");

            Status = AssessmentStatus.Processing;
            ProcessingStartedAt = DateTimeOffset.UtcNow;

            RaiseDomainEvent(
                new AssessmentProcessingStartedDomainEvent(
                    Id,
                    ProcessingStartedAt.Value
                )
            ); // Probably Send a domain event to indicate that processing has started

        }

        public void Complete(Guid decisionId)
        {
            if (Status != AssessmentStatus.Processing)
                throw new InvalidOperationException(
                    "Assessment must be processing before completion.");

            if (decisionId == Guid.Empty)
                throw new ArgumentException("DecisionId is required.");

            var completedAt = DateTimeOffset.UtcNow;

            DecisionId = decisionId;
            Status = AssessmentStatus.Completed;
            CompletedAt = completedAt;

            RaiseDomainEvent(
                new AssessmentCompletedDomainEvent(
                    Id,
                    DecisionId,
                    completedAt
                )
            );
        }

        public void Fail(string failureReason)
        {
            if (Status != AssessmentStatus.Processing)
                throw new InvalidOperationException(
                    "Assessment must be processing before failure.");

            if (string.IsNullOrWhiteSpace(failureReason))
                throw new ArgumentException("Failure reason is required.");

            Status = AssessmentStatus.Failed;
            FailureReason = failureReason;
            CompletedAt = DateTimeOffset.UtcNow;
            RaiseDomainEvent(
                new AssessmentFailedDomainEvent(
                    Id,
                    failureReason,
                    DateTimeOffset.UtcNow
                )
            );
        }

        public void MarkServiceUnavailable(string serviceName)
        {
            if (Status != AssessmentStatus.Processing)
                throw new InvalidOperationException(
                    "Assessment must be processing before marking as service unavailable.");

            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name is required.");

            var occurredAt = DateTimeOffset.UtcNow;

            Status = AssessmentStatus.ServiceUnavailable;
            FailureReason = $"{serviceName} is unavailable.";

            RaiseDomainEvent(
                new AssessmentServiceUnavailableDomainEvent(
                    Id,
                    serviceName,
                    occurredAt
                )
            );
        }
    }
}
