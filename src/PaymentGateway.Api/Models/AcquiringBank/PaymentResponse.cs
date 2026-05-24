using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.AcquiringBank
{
    public class PaymentResponse
    {
        public bool Authorized { get; set; }
        [JsonPropertyName("authorization_code")]
        public string AuthorizationCode { get; set; } = "";
    }
}
