using FluentValidation;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Merchant;

namespace PaymentGateway.Api.Validation.Merchant
{
    public class PaymentResponseValidator : AbstractValidator<PaymentResponse>
    {
        /*
         Domain rules for validating a payment response to a merchant
         */
        public PaymentResponseValidator(IISOCurrencyCodes iSOCurrencyCodes)
        {
            /**
             * Payment gateways cannot return a full card number as this is a serious compliance risk. 
             * However, it is fine to return the last four digits of a card
             */

            RuleFor(payment => payment.CardNumberLastFour)
                .Length(4)
                .WithMessage("Card number last four must be exactly 4 characters long.")
                .Must(cardNumber => int.TryParse(cardNumber, out _))
                .WithMessage("Card number last four must contain only numeric characters.");

            RuleFor(payment => payment.Currency)
                .Must(iSOCurrencyCodes.IsValidCurrencyCode)
                .WithMessage("Invalid currency code.");
        }
    }
}
