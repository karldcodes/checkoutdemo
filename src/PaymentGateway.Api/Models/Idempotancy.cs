using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Models
{
    public class Idempotancy
    {
        public Guid EntryId { get; set; }
        public string Key { get; set; } = "";
        public IdempotencyStatus Status { get; set; } = IdempotencyStatus.Processing;
        public int? ResponseStatusCode { get; set; }
        public string? ResponseBody { get; set; }
    }
}
