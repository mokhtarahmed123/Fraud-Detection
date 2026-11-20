namespace FraudAssessment.Domain.Enums
{
    public enum AssessmentStatus
    {
        Received = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        ServiceUnavailable = 5,
        QueueTimeout = 6,
        ProcessingTimeout = 7

    }
}
