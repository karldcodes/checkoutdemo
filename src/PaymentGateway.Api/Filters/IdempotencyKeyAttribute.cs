using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Api.Filters
{
    public class IdempotencyKeyAttribute : TypeFilterAttribute
    {
        public IdempotencyKeyAttribute() : base(typeof(IdempotencyKeyFilter))
        {
        }
    }
}
