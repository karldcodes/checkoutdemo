using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;

using PaymentGateway.Api.Interfaces;

namespace PaymentGateway.Api.Metrics
{
    public class PaymentMetrics : IPaymentMetrics
    {
        private readonly Histogram<double> _paymentProcessingDuration;
        private readonly Histogram<HttpStatusCode> _paymentHttpStatusCodes;
        private readonly Counter<int> _paymentProcessingSuccess;
        private readonly Counter<int> _paymentProcessingDeclined;
        private readonly Counter<int> _paymentProcessingRejected;
        private readonly Counter<int> _paymentProcessCalls;
        public PaymentMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("PaymentGateway.Api.Metrics");

            _paymentHttpStatusCodes = meter.CreateHistogram<HttpStatusCode>(
                name: "payment_http_status_codes",
                unit: "status_code",
                description: "HTTP status codes returned by payment processing");

            _paymentProcessingDuration = meter.CreateHistogram<double>(
                name: "payment_processing_duration",
                unit: "ms",
                description: "Duration of payment processing in milliseconds");
            _paymentProcessingSuccess = meter.CreateCounter<int>(
                name: "payment_processing_success",
                unit: "count",
                description: "Number of successful payment processing attempts");
            _paymentProcessingDeclined = meter.CreateCounter<int>(
                name: "payment_processing_declined",
                unit: "count",
                description: "Number of declined payment processing attempts");
            _paymentProcessingRejected = meter.CreateCounter<int>(
                name: "payment_processing_rejected",
                unit: "count",
                description: "Number of rejected payment processing attempts");
            _paymentProcessCalls = meter.CreateCounter<int>(
                name: "payment_process_calls",
                unit: "count",
                description: "Number of calls to the payment processing method");
        }

        public void RecordPaymentHttpStatusCode(HttpStatusCode statusCode)
        {
            _paymentHttpStatusCodes.Record(statusCode);
        }

        public void RecordPaymentProcessingDuration(double durationMs)
        {
            _paymentProcessingDuration.Record(durationMs);
        }

        public void RecordPaymentProcessingSuccess()
        {
            _paymentProcessingSuccess.Add(1);
        }

        public void RecordPaymentProcessingDeclined()
        {
            _paymentProcessingDeclined.Add(1);
        }

        public void RecordPaymentProcessCall()
        {
            _paymentProcessCalls.Add(1);
        }

        public void RecordPaymentProcessingRejected()
        {
            _paymentProcessingRejected.Add(1);
        }
    }
}
