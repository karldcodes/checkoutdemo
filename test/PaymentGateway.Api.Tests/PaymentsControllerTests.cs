using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentValidation;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.HttpClients;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Metrics;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Merchant;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation.Merchant;

namespace PaymentGateway.Api.Tests;


/*
 * POST /payments with valid request returns 200/201
POST /payments with invalid card number returns 400
POST /payments with expired card returns 400
POST /payments with invalid currency returns 400
POST /payments with zero/negative amount returns 400
POST /payments with bad CVV returns 400
 */

public class FakeAcquiringBankClient : IAcquiringBankClient
{
    public Task<HttpResponseMessage> SendPaymentAsync(Models.AcquiringBank.PaymentRequest request)
    {
        return Task.FromResult(new HttpResponseMessage() { StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new Models.AcquiringBank.PaymentResponse
            {
                Authorized = true,
                AuthorizationCode = Guid.NewGuid().ToString()
            })
        });
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "integration-test-user"),
            new Claim(ClaimTypes.Role, "Merchant")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class FakePaymentsRepository : IPaymentsRepository
{
    public readonly List<Payment> _payments = [];
    public Task Add(Payment payment)
    {
        _payments.Add(payment);
        return Task.CompletedTask;
    }
    public Task<Payment?> Get(Guid id)
    {
        return Task.FromResult(_payments.FirstOrDefault(p => p.Id == id));
    }
}

public class PaymentsControllerTests
{

    [Fact]
    public async Task Payments_validRequest_returns200()
    {
        // Arrange
        var paymentsRepository = new FakePaymentsRepository();

        var bankClient = new Mock<IAcquiringBankClient>();

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => {
                services.RemoveAll<IPaymentsRepository>();
                services.AddSingleton<IPaymentsRepository>(paymentsRepository);
                services.AddSingleton<IAcquiringBankClient, FakeAcquiringBankClient>();
                services.AddSingleton<IISOCurrencyCodes, ISOCurrencyCodes>();
                services.AddScoped<IValidator<PaymentRequest>, PaymentRequestValidator>();
                services.AddSingleton<IIdempotancyRepository, IdempotancyRepository>();
                services.AddSingleton<IPaymentMetrics, PaymentMetrics>();
                services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    options => { });

            }))
            .CreateClient();

        var request = new PaymentRequest
        {
            Amount = 100,
            CardNumber = "4111111111111111",
            Currency = "GBP",
            ExpiryMonth = DateTime.Now.Month,
            ExpiryYear = DateTime.Now.Year + 1,
            CVV = "123"
        };

        var httpRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/payments");

        httpRequest.Headers.Add(
        "Idempotency-Key",
        Guid.NewGuid().ToString());

        httpRequest.Content = JsonContent.Create(request);


        // Act
        var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(paymentsRepository._payments);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Authorized.ToString(), paymentResponse?.Status);
    }
}