using FraudAssessment.Domain.Entities;

namespace FraudAssessment.Application.Interface
{
    public interface IIdempotencyRepository
    {
        Task<IdempotencyRecord> GetByClientIdAndKeyAsync(string clientId, string key, CancellationToken cancellationToken);
        Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken);
        Task DeleteExpiredAsync(CancellationToken cancellationToken);
    }
}
