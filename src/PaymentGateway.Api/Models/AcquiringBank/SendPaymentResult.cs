namespace PaymentGateway.Api.Models.AcquiringBank
{
    public class SendPaymentResult
    {
        public bool IsSuccessful { get; init; }

        public PaymentStatus Status { get; init; }

        public PaymentResponse? PaymentResponse { get; init; }
    }
}
