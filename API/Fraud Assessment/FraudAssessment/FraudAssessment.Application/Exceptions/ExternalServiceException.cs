namespace FraudAssessment.Application.Exceptions
{
    public class ExternalServiceException : Exception
    {
        public ExternalServiceException(string message, Exception? innerException = null)
            : base(message, innerException) { }
    }
}
