using System.Net;

namespace PaymentGateway.Api.Interfaces
{
    public interface IPaymentMetrics
    {
        void RecordPaymentProcessCall();
        void RecordPaymentProcessingDuration(double durationMs);
        void RecordPaymentProcessingDeclined();
        void RecordPaymentProcessingSuccess();
        void RecordPaymentProcessingRejected();
        void RecordPaymentHttpStatusCode(HttpStatusCode statusCode);
    }
}