using System.Collections.ObjectModel;
using System.Globalization;

using PaymentGateway.Api.Interfaces;

namespace PaymentGateway.Api.Services
{
    public class ISOCurrencyCodes : IISOCurrencyCodes
    {
        private HashSet<string> CurrencyCodes { get; }

        public ISOCurrencyCodes()
        {
            CurrencyCodes = CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .Select(culture => new RegionInfo(culture.Name))
                .Select(region => region.ISOCurrencySymbol)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public bool IsValidCurrencyCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return CurrencyCodes.Contains(code);
        }
    }
}
