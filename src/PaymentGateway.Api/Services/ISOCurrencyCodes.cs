using System.Collections.ObjectModel;
using System.Globalization;

using PaymentGateway.Api.Interfaces;

namespace PaymentGateway.Api.Services
{
    public class ISOCurrencyCodes : IISOCurrencyCodes
    {
        private ReadOnlyCollection<string> CurrencyCodes { get; }

        public ISOCurrencyCodes()
        {
            // Get all unique ISO currency codes from the specific cultures
            // Should this list need to be updated in the future, it can be
            // done by updating the .NET runtime or by implementing a custom
            // list of currency codes
            CurrencyCodes = CultureInfo
            .GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .Select(region => region.ISOCurrencySymbol)
            .Distinct()
            .ToList()
            .AsReadOnly();
        }

        public bool IsValidCurrencyCode(string code)
        {
            return CurrencyCodes.Contains(code);
        }
    }
}
