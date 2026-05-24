using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Interfaces
{
    public interface IIdempotancyRepository : IRepositoryAdd<Idempotancy>, IRepositoryGet<Idempotancy, string>
    {
        Task SaveResponseAsync(Idempotancy item);
        Task UpdateStatus(string key, IdempotencyStatus status);
    }
}
