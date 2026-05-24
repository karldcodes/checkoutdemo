using System.Collections.Immutable;

using FluentValidation;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Merchant;

namespace PaymentGateway.Api.Validation.Merchant
{

    public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
    {
        private bool IsFutureDate(int month, int year)
        {
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            return (year > currentYear) || (year == currentYear && month >= currentMonth);
        }

        /*
         Domain rules for validating a payment request from a merchant
         */
        public PaymentRequestValidator(IISOCurrencyCodes iSOCurrencyCodes)
        {
            RuleFor(payment => payment.CardNumber)
                .NotEmpty()
                .WithMessage("Card number is required.")
                .Length(14, 19)
                .WithMessage("Card number must be between 14 and 19 characters long.")
                .Must(cardNumber => !string.IsNullOrWhiteSpace(cardNumber) && cardNumber.All(char.IsDigit))
                .WithMessage("Card number must contain only numeric characters.");

            RuleFor(payment => payment.ExpiryMonth)
                .NotEmpty()
                .WithMessage("Expiry month is required.")
                .InclusiveBetween(1, 12)
                .WithMessage("Expiry month must be between 1 and 12.");

            RuleFor(payment => payment.ExpiryYear)
            .NotEmpty()
            .WithMessage("Expiry year is required.")
            .InclusiveBetween(DateTime.UtcNow.Year, DateTime.UtcNow.Year + 20) // Assuming cards won't have an expiry date more than 20 years in the future
            .WithMessage("Expiry year is invalid.");

            When(payment => payment.ExpiryMonth != 0 && payment.ExpiryYear != 0, () =>
            {
                RuleFor(payment => payment)
                    .Must(payment => IsFutureDate(payment.ExpiryMonth, payment.ExpiryYear))
                    .WithMessage("Card has expired.");
            });

            RuleFor(payment => payment.Currency)
                .NotEmpty()
                .WithMessage("Currency is required.")
                .Length(3, 3)
                .WithMessage("Currency must be a 3-character code.")
                .Must(iSOCurrencyCodes.IsValidCurrencyCode)
                .WithMessage("Invalid currency code.");

            RuleFor(payment => payment.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(payment => payment.CVV)
                .NotEmpty()
                .WithMessage("CVV is required.")
                .Length(3, 4)
                .WithMessage("CVV must be 3 or 4 characters long.")
                .Must(cvv => !string.IsNullOrWhiteSpace(cvv) && cvv.All(char.IsDigit))
                .WithMessage("CVV must contain only numeric characters.");
        }
    }
}
