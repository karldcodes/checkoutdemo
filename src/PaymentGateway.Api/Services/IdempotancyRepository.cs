using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services
{
    public class IdempotancyRepository: IIdempotancyRepository
    {
        public List<Idempotancy> Keys = [];

        public async Task Add(Idempotancy item)
        {
            Keys.Add(item);
        }

        public async Task SaveResponseAsync(Idempotancy item)
        {
            var idempotancy = Keys.FirstOrDefault(k => k.Key == item.Key);
            idempotancy?.Status = item.Status;
            idempotancy?.ResponseStatusCode = item.ResponseStatusCode;
            idempotancy?.ResponseBody = item.ResponseBody;
        }

        public async Task UpdateStatus(string key, IdempotencyStatus status)
        {
            var idempotancy = Keys.FirstOrDefault(k => k.Key == key);
            if (idempotancy is not null)
            {
                idempotancy.Status = status;
            }
        }

        public async Task<Idempotancy?> Get(string id)
        {
            return Keys.FirstOrDefault(k => k.Key == id);
        }
    }
}
