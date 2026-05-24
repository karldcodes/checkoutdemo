using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Controllers;

public static partial class PaymentsLog
{
    // event ids 1000-1099 = Payments

    [LoggerMessage(
        EventId = 1000,
        EventName = "ReceivedPaymentRequest",
        Level = LogLevel.Information,
        Message = "Processing payment request for amount {Amount} {Currency}")]
    internal static partial void ReceivedPaymentRequest(
        ILogger logger,
        decimal amount,
        string currency);

    [LoggerMessage(
        EventId = 1001,
        EventName = "ValidationFailure",
        Level = LogLevel.Warning,
        Message = "Payment validation failed for amount{Errors}")]
    internal static partial void PaymentValidationFailure(
        ILogger logger,
        string errors);

    [LoggerMessage(
        EventId = 1002,
        EventName = "ProcessedSuccessfully",
        Level = LogLevel.Information,
        Message = "Payment processed with status {Status} and id {PaymentId}")]
    internal static partial void PaymentProcessedStatus(
        ILogger logger,
        PaymentStatus status,
        Guid paymentId);

    [LoggerMessage(
        EventId = 1003,
        EventName = "ReceivedGetPaymentRequest",
        Level = LogLevel.Information,
        Message = "Received request to get payment with id {PaymentId}")]
    internal static partial void ReceivedGetPaymentRequest(
        ILogger logger,
        Guid paymentId);

    [LoggerMessage(
        EventId = 1004,
        EventName = "PaymentNotFound",
        Level = LogLevel.Information,
        Message = "Payment with id {PaymentId} not found")]
    internal static partial void PaymentNotFound(
        ILogger logger,
        Guid paymentId);

    [LoggerMessage(
        EventId = 1005,
        EventName = "PaymentFound",
        Level = LogLevel.Information,
        Message = "Payment with id {PaymentId} found")]
    internal static partial void PaymentFound(
        ILogger logger,
        Guid paymentId);

    [LoggerMessage(
        EventId = 1006,
        EventName = "PaymentRejected",
        Level = LogLevel.Information,
        Message = "Payment rejected {PaymentId}")]
    internal static partial void PaymentRejected(
        ILogger logger,
        Guid paymentId);

    [LoggerMessage(
        EventId = 1007,
        EventName = "PaymentDeclined",
        Level = LogLevel.Information,
        Message = "Payment declined {PaymentId}")]
    internal static partial void PaymentDeclined(
        ILogger logger,
        Guid paymentId);
}
