using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.AcquiringBank;

using Polly.Timeout;

namespace PaymentGateway.Api.Services
{
    public class AcquiringBank : IAcquiringBank
    {
        private readonly IAcquiringBankClient _httpClient;
        private readonly ILogger<AcquiringBank> _logger;

        public AcquiringBank(IAcquiringBankClient httpClient, ILogger<AcquiringBank> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<SendPaymentResult> SendPayment(PaymentRequest request)
        {
            try
            {
                var response = await _httpClient.SendPaymentAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogInformation("Invalid payment request received");
                    return new SendPaymentResult
                    {
                        Status = PaymentStatus.Rejected
                    };
                }

                if (response.IsSuccessStatusCode)
                {
                    var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
                    if (paymentResponse is null)
                    {
                        throw new InvalidOperationException("Failed to deserialize payment response from acquiring bank");
                    }

                    if (paymentResponse.Authorized)
                    {
                        _logger.LogInformation("Payment authorized by acquiring bank");
                        return new SendPaymentResult
                        {
                            Status = PaymentStatus.Authorized,
                            PaymentResponse = paymentResponse
                        };
                    }
                    else
                    {
                        _logger.LogInformation("Payment declined by acquiring bank");
                        return new SendPaymentResult
                        {
                            Status = PaymentStatus.Declined,
                            PaymentResponse = paymentResponse
                        };
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Acquiring bank returned status code {StatusCode}",
                        response.StatusCode);

                    return new SendPaymentResult
                    {
                        Status = PaymentStatus.Rejected,
                    };
                }                

            }
            // Depends how explict we need to be with recording errors from the retry policy - we could have a single catch block for all exceptions,
            // but this way we can be more specific about the error and log it accordingly
            catch (TimeoutRejectedException ex)
            {
                _logger.LogError(ex, "Bank request timed out");
                return new SendPaymentResult
                {
                    Status = PaymentStatus.Rejected
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling bank");
                return new SendPaymentResult
                {
                    Status = PaymentStatus.Rejected
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to deserialize payment response from acquiring bank");
                return new SendPaymentResult
                {
                    Status = PaymentStatus.Rejected
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                return new SendPaymentResult
                {
                    Status = PaymentStatus.Rejected
                };
            }
        }
    }
}
