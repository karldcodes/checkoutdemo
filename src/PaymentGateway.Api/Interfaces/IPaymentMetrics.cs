using System.Net;

using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Interfaces
{
    public interface IPaymentMetrics
    {
        void RecordPaymentProcessCall();
        void RecordPaymentProcessingDuration(double durationMs);
        void RecordPaymentHttpStatusCode(HttpStatusCode statusCode);
        void RecordPaymentStatus(PaymentStatus status);
    }
}