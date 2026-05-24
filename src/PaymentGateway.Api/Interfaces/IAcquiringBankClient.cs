using PaymentGateway.Api.Models.AcquiringBank;

namespace PaymentGateway.Api.Interfaces
{
    public interface IAcquiringBankClient
    {
        Task<HttpResponseMessage> SendPaymentAsync(PaymentRequest request);
    }
}
