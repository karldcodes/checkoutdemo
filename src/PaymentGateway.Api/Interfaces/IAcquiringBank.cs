using PaymentGateway.Api.Models.AcquiringBank;

namespace PaymentGateway.Api.Interfaces
{
    public interface IAcquiringBank
    {
        Task<SendPaymentResult> SendPayment(PaymentRequest request);
    }
}