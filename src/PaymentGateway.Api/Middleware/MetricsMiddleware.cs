using System.Diagnostics;
using System.Net;

using Microsoft.AspNetCore.Mvc.Controllers;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Interfaces;

namespace PaymentGateway.Api.Middleware;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPaymentMetrics _paymentMetrics;

    public MetricsMiddleware(RequestDelegate next, IPaymentMetrics paymentMetrics)
    {
        _next = next;
        _paymentMetrics = paymentMetrics;
    }

    private string GetActionName(HttpContext context)
    {
        var controllerActionDescriptor = context?
            .GetEndpoint()?
            .Metadata
            .GetMetadata<ControllerActionDescriptor>();
        return controllerActionDescriptor?.ActionName ?? "Unknown";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        var actionName = GetActionName(context);
        if (actionName == nameof(PaymentsController.PostPaymentAsync))
        {
            _paymentMetrics.RecordPaymentProcessCall();
        }
        

        await _next(context);

        sw.Stop();

        _paymentMetrics.RecordPaymentProcessingDuration(sw.Elapsed.TotalMilliseconds);
        _paymentMetrics.RecordPaymentHttpStatusCode((HttpStatusCode)context.Response.StatusCode);
    }
}
