namespace PaymentGateway.Api.Models.Merchant
{
    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CardNumber { get; set; } = "";
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string Currency { get; set; } = "";
        public int Amount { get; set; }
        public string Cvv { get; set; } = "";
        public PaymentStatus Status { get; set; }
        public string AuthorizationCode { get; set; } = "";
    }
}
