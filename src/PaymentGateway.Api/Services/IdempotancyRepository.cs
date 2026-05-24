using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services
{
    public class IdempotancyRepository: IIdempotancyRepository
    {
        private readonly List<Idempotancy> _keys = [];

        public async Task Add(Idempotancy item)
        {
            _keys.Add(item);
        }

        public async Task SaveResponseAsync(Idempotancy item)
        {
            var idempotancy = _keys.FirstOrDefault(k => k.Key == item.Key);
            idempotancy?.Status = item.Status;
            idempotancy?.ResponseStatusCode = item.ResponseStatusCode;
            idempotancy?.ResponseBody = item.ResponseBody;
        }

        public async Task UpdateStatus(string key, IdempotencyStatus status)
        {
            var idempotancy = _keys.FirstOrDefault(k => k.Key == key);
            if (idempotancy is not null)
            {
                idempotancy.Status = status;
            }
        }

        public async Task<Idempotancy?> Get(string id)
        {
            return _keys.FirstOrDefault(k => k.Key == id);
        }
    }
}
