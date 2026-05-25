using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Merchant;

namespace PaymentGateway.Api.Filters
{
    // IAsyncResourceFilter runs before and after the entire action execution pipeline,
    public class IdempotencyKeyFilter : IAsyncResourceFilter
    {
        private readonly IIdempotancyRepository _idempotancyRepository;

        public IdempotencyKeyFilter(IIdempotancyRepository idempotancyRepository)
        {
            _idempotancyRepository = idempotancyRepository;
        }

        private string GetIdempotencyKey(ResourceExecutingContext context)
        {
            if (context.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var key))
            {
                return key.ToString();
            }
            return string.Empty;
        }


        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            // check and validate the idempotency key header, if missing or invalid, set context.Result to an error response and return
            var key = GetIdempotencyKey(context);

            if (string.IsNullOrWhiteSpace(key))
            {
                // could return the expected type?
                //var currnetRequest = context.HttpContext.Request.ReadFromJsonAsync<PaymentRequest>();

                //new PaymentResponse
                //{
                //    Id = payment.Id,
                //    Amount = request.Amount,
                //    Currency = request.Currency,
                //    CardNumberLastFour = MaskCardNumber(request.CardNumber),
                //    ExpiryMonth = request.ExpiryMonth,
                //    ExpiryYear = request.ExpiryYear,
                //    Status = paymentStatus.ToString(),
                //};

                context.Result = new BadRequestObjectResult("Missing Idempotency-Key header");
                return;
            }

            // check if the key has already been processed, if so, set context.Result to a conflict response and return
            var entry = await _idempotancyRepository.Get(key);

            if (entry is null)
            {
                // No existing payment with this idempotency key, allow the request to proceed
                await _idempotancyRepository.Add(new Idempotancy() { Key = key});
            }

            if (entry?.Status == IdempotencyStatus.Processing)
            {
                // A request with this idempotency key is currently being processed, return a conflict response
                context.Result = new ConflictObjectResult("Request is currently being processed");
                return;
            }

            if (entry?.Status == IdempotencyStatus.Failed)
            {
                // A request with this idempotency key has previously failed, allow the request to proceed
                //return;
            }

            if (entry?.Status == IdempotencyStatus.Completed)
            {
                // A request with this idempotency key has already been completed, return the cached response
                context.Result = new OkObjectResult(entry.ResponseBody);
                return;
            }

            var executedContext = await next();

            // examine the executedContext to determine if the request was successful (e.g., check for exceptions or response status code)
            // update the idempotency status in the repository based on the outcome of the request (e.g., set to Completed if successful, or Failed if there was an error)
            if (executedContext.Exception is null &&
                context.HttpContext.Response.StatusCode is >= 200 and < 300)
            {
                // if the request was successful, update the idempotency status to Completed and cache the
                // response body and status code for future requests with the same key
                if (executedContext.Result is ObjectResult objectResult)
                {
                    var json = JsonSerializer.Serialize(objectResult.Value);

                    await _idempotancyRepository.SaveResponseAsync(
                        new Idempotancy
                        {
                            Key = key,
                            Status = IdempotencyStatus.Completed,
                            ResponseStatusCode = objectResult.StatusCode ?? 200,
                            ResponseBody = json
                        });
                }
            }
            else
            {
                // todo handle exceptions and errors more robustly, for example by logging the error details and providing more specific error responses based on the type of failure
                await _idempotancyRepository.UpdateStatus(key, IdempotencyStatus.Failed);
            }
        }
    }
}
