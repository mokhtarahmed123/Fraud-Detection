using FraudAssessment.Domain.Entities;

namespace FraudAssessment.Application.Interface
{
    public interface IAssessmentRepository
    {
        Task<Assessment?> GetAssessmentByIdAsync(Guid assessmentId, CancellationToken cancellationToken);

        Task AddAssessmentAsync(Assessment assessment, CancellationToken cancellationToken);
        Task UpdateAssessmentAsync(Guid assessmentId, Assessment assessment, CancellationToken cancellationToken);
        Task<IReadOnlyList<Assessment>> GetAssessmentsByClientIdAsync(
                    string clientId,
                    CancellationToken cancellationToken = default);

        Task<bool> TryUpdateReceivedAssessmentAsync(
    Guid assessmentId,
    Assessment assessment,
    CancellationToken cancellationToken);
    }
}
