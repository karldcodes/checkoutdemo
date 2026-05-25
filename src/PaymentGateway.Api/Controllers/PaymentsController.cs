using System.Net;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Filters;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Merchant;

using AcquiringBank = PaymentGateway.Api.Models.AcquiringBank;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class PaymentsController : ControllerBase // inheriting from controllerbase as we dont need views
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IValidator<PaymentRequest> _paymentRequestValidator;
    private readonly IPaymentMetrics _paymentMetrics;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IAcquiringBank _acquiringBank;

    public PaymentsController(IPaymentsRepository paymentsRepository, 
        IValidator<PaymentRequest> paymentRequestValidator,
        IPaymentMetrics paymentMetrics,
        ILogger<PaymentsController> logger,
        IAcquiringBank acquiringBank)
    {
        _paymentsRepository = paymentsRepository;
        _paymentRequestValidator = paymentRequestValidator;
        _paymentMetrics = paymentMetrics;
        _logger = logger;
        _acquiringBank = acquiringBank;
    }

    private string MaskCardNumber(string cardNumber)
    {
        if (cardNumber.Length < 4)
            return "****"; // Not a valid card number, mask completely
        return new string('*', cardNumber.Length - 4) + cardNumber[^4..];
    }


    // Allow only users with merchant claim to start a payment
    [Authorize(Roles = "Merchant")]
    [IdempotencyKey] // custom filter to handle idempotency key validation and processing
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> PostPaymentAsync([FromBody] PaymentRequest request)
    {
        PaymentsLog.ReceivedPaymentRequest(_logger, request.Amount, request.Currency);

        var paymentStatus = PaymentStatus.Declined;
        var result = _paymentRequestValidator.Validate(request);
        AcquiringBank.SendPaymentResult? acquiringBankResponse = null;

        if (!result.IsValid)
        {
            // reject
            PaymentsLog.PaymentValidationFailure(_logger, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
            _paymentMetrics.RecordPaymentStatus(PaymentStatus.Rejected);

            paymentStatus = PaymentStatus.Rejected;
        }
        else
        {
            // call acquiring bank as validation is successful
            acquiringBankResponse = await _acquiringBank.SendPayment(new AcquiringBank.PaymentRequest
            {
                Amount = request.Amount,
                Currency = request.Currency,
                CardNumber = request.CardNumber,
                ExpiryDate = request.ExpiryMonth + "/" + request.ExpiryYear,
                Cvv = request.Cvv
            });
            paymentStatus = acquiringBankResponse.Status;
        }

        // save payment to repository
        var payment = new Payment
        {
            Amount = request.Amount,
            Currency = request.Currency,
            CardNumber = MaskCardNumber(request.CardNumber),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Cvv = request.Cvv,
            Status = paymentStatus,
            AuthorizationCode = acquiringBankResponse?.PaymentResponse?.AuthorizationCode ?? ""
        };

        await _paymentsRepository.Add(payment);

        PaymentsLog.PaymentProcessedStatus(_logger, paymentStatus, payment.Id);
        _paymentMetrics.RecordPaymentStatus(paymentStatus);


        var statusCode = paymentStatus switch
        {
            PaymentStatus.Authorized => HttpStatusCode.OK,
            PaymentStatus.Declined => HttpStatusCode.BadRequest, //HttpStatusCode.PaymentRequired,
            PaymentStatus.Rejected => HttpStatusCode.BadRequest, //HttpStatusCode.PaymentRequired,
            _ => HttpStatusCode.BadRequest
        };

        return StatusCode((int)statusCode, new PaymentResponse
        {
            Id = payment.Id,
            Amount = request.Amount,
            Currency = request.Currency,
            CardNumberLastFour = MaskCardNumber(request.CardNumber),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Status = paymentStatus.ToString(),
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponse?>> GetPaymentAsync(Guid id)
    {
        PaymentsLog.ReceivedGetPaymentRequest(_logger, id);
        //todo add metrics for this endpoint as well
        var payment = await _paymentsRepository.Get(id);

        if (payment == null)
        {
            PaymentsLog.PaymentNotFound(_logger, id);
            return NotFound();
        }

        PaymentsLog.PaymentFound(_logger, id);
        return Ok(new PaymentResponse
        {
            Id = payment.Id,
            Amount = payment.Amount,
            Currency = payment.Currency,
            CardNumberLastFour = payment.CardNumber,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Status = payment.Status.ToString(),
        });
    }
}