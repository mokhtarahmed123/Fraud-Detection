namespace FraudAssessment.Application.Exceptions
{
    public class IdempotencyConflictException : Exception
    {
        public IdempotencyConflictException()
            : base("Idempotency key was already used with a different request.")
        {
        }
    }
}
