namespace PaymentGateway.Api.Interfaces
{
    public interface IISOCurrencyCodes
    {
        bool IsValidCurrencyCode(string code);
    }
}