using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Metrics
{
    public class PaymentMetrics : IPaymentMetrics
    {
        private readonly Histogram<double> _paymentProcessingDuration;
        private readonly Histogram<HttpStatusCode> _paymentHttpStatusCodes;
        private readonly Histogram<PaymentStatus> _paymentStatusCodes;
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

            _paymentStatusCodes = meter.CreateHistogram<PaymentStatus>(
                name: "payment_status_codes",
                unit: "status_code",
                description: "Payment status codes");

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

        public void RecordPaymentStatus(PaymentStatus status)
        {
            _paymentStatusCodes.Record(status);
        }

        public void RecordPaymentProcessCall()
        {
            _paymentProcessCalls.Add(1);
        }
    }
}
