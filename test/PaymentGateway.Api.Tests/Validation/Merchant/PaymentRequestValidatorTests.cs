using FluentValidation.TestHelper;

using Moq;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Merchant;
using PaymentGateway.Api.Validation.Merchant;


namespace PaymentGateway.Api.Tests.Validation.Merchant
{
    public class PaymentRequestValidatorTests
    {
        private readonly Mock<IISOCurrencyCodes> _currencyCodesMock;
        private readonly PaymentRequestValidator _validator;

        public PaymentRequestValidatorTests()
        {
            _currencyCodesMock = new Mock<IISOCurrencyCodes>();

            _currencyCodesMock
                .Setup(x => x.IsValidCurrencyCode("GBP"))
                .Returns(true);

            _currencyCodesMock
                .Setup(x => x.IsValidCurrencyCode(It.Is<string>(c => c != "GBP")))
                .Returns(false);

            _validator = new PaymentRequestValidator(_currencyCodesMock.Object);
        }

        private static PaymentRequest ValidRequest() => new()
        {
            CardNumber = "4111111111111111",
            ExpiryMonth = DateTime.UtcNow.Month,
            ExpiryYear = DateTime.UtcNow.Year,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        [Fact]
        public void Should_Pass_When_Request_Is_Valid()
        {
            var request = ValidRequest();

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_Fail_When_CardNumber_Is_Empty(string cardNumber)
        {
            var request = ValidRequest();
            request.CardNumber = cardNumber;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.CardNumber);
        }

        [Theory]
        [InlineData("1234567890123")]
        [InlineData("12345678901234567890")]
        public void Should_Fail_When_CardNumber_Length_Is_Invalid(string cardNumber)
        {
            var request = ValidRequest();
            request.CardNumber = cardNumber;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.CardNumber);
        }

        [Fact]
        public void Should_Fail_When_CardNumber_Contains_NonNumeric_Characters()
        {
            var request = ValidRequest();
            request.CardNumber = "411111111111abcd";

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.CardNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void Should_Fail_When_ExpiryMonth_Is_Invalid(int month)
        {
            var request = ValidRequest();
            request.ExpiryMonth = month;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1999)]
        public void Should_Fail_When_ExpiryYear_Is_Invalid(int year)
        {
            var request = ValidRequest();
            request.ExpiryYear = year;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryYear);
        }

        [Fact]
        public void Should_Fail_When_ExpiryYear_Is_More_Than_20_Years_In_Future()
        {
            var request = ValidRequest();
            request.ExpiryYear = DateTime.UtcNow.Year + 21;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryYear);
        }

        [Fact]
        public void Should_Fail_When_Card_Has_Expired()
        {
            var expiredDate = DateTime.UtcNow.AddMonths(-1);

            var request = ValidRequest();
            request.ExpiryMonth = expiredDate.Month;
            request.ExpiryYear = expiredDate.Year;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Should_Pass_When_ExpiryDate_Is_Current_Month()
        {
            var request = ValidRequest();
            request.ExpiryMonth = DateTime.UtcNow.Month;
            request.ExpiryYear = DateTime.UtcNow.Year;

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("GB")]
        [InlineData("GBPP")]
        [InlineData("XXX")]
        public void Should_Fail_When_Currency_Is_Invalid(string currency)
        {
            var request = ValidRequest();
            request.Currency = currency;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Currency);
        }

        [Fact]
        public void Should_Pass_When_Currency_Is_Valid()
        {
            var request = ValidRequest();
            request.Currency = "GBP";

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Currency);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Should_Fail_When_Amount_Is_Not_Greater_Than_Zero(int amount)
        {
            var request = ValidRequest();
            request.Amount = amount;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Amount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("12")]
        [InlineData("12345")]
        [InlineData("12A")]
        [InlineData("   ")]
        public void Should_Fail_When_CVV_Is_Invalid(string cvv)
        {
            var request = ValidRequest();
            request.Cvv = cvv;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Cvv);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("1234")]
        public void Should_Pass_When_CVV_Is_Valid(string cvv)
        {
            var request = ValidRequest();
            request.Cvv = cvv;

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Cvv);
        }
    }
}
