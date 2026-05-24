using System.Text.Json;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.AcquiringBank;

namespace PaymentGateway.Api.HttpClients
{
    public class AcquiringBankClient : IAcquiringBankClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _acquiringBankUrl;

        public AcquiringBankClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            // in a real application, you would want to use a more robust configuration management solution
            // and also consider using a secret manager for sensitive information
            _acquiringBankUrl = config["AcquiringBank:Url"] ?? throw new InvalidOperationException("Acquiring bank URL is not configured");
        }

        private JsonContent CreateJsonContent(PaymentRequest request)
        {
            // explictly define the naming policy as the acquiring bank's API expects camelCase properties
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonContent.Create(request, options: serializeOptions);
        }

        public async Task<HttpResponseMessage> SendPaymentAsync(
            PaymentRequest request)
        {
            var content = CreateJsonContent(request);

            return await _httpClient.PostAsync(
                _acquiringBankUrl,
                content);
        }
    }
}
