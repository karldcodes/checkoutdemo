using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.AcquiringBank;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Services
{
    public class AcquiringBankTests
    {
        [Fact]
        public async Task SendPayment_WhenBankReturns200Authorized_ReturnsSuccess()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PaymentResponse
                {
                    Authorized = true
                })
            };

            var bankClient = new Mock<IAcquiringBankClient>();
            bankClient
                .Setup(x => x.SendPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(httpResponse);

            var service = new AcquiringBank(bankClient.Object, Mock.Of<ILogger<AcquiringBank>>());

            // Act
            var result = await service.SendPayment(new PaymentRequest());

            // Assert
            Assert.NotNull(result.PaymentResponse);
            Assert.True(result.PaymentResponse.Authorized);
            Assert.True(result.Status == Models.PaymentStatus.Authorized);
        }

        [Fact]
        public async Task SendPayment_WhenBankReturns200Unauthorized_ReturnsSuccessWithUnauthorizedResponse()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PaymentResponse
                {
                    Authorized = false
                })
            };

            var bankClient = new Mock<IAcquiringBankClient>();
            bankClient
                .Setup(x => x.SendPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(httpResponse);

            var service = new AcquiringBank(bankClient.Object, Mock.Of<ILogger<AcquiringBank>>());

            // Act
            var result = await service.SendPayment(new PaymentRequest());

            // Assert
            Assert.NotNull(result.PaymentResponse);
            Assert.False(result.PaymentResponse.Authorized);
            Assert.True(result.Status == Models.PaymentStatus.Declined);
        }

        [Fact]
        public async Task SendPayment_WhenBankReturns400_ReturnsFailure()
        {
            // Arrange
            var bankClient = new Mock<IAcquiringBankClient>();

            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("")
            };

            bankClient
                .Setup(x => x.SendPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(httpResponse);

            var service = new AcquiringBank(bankClient.Object, Mock.Of<ILogger<AcquiringBank>>());

            // Act
            var result = await service.SendPayment(new PaymentRequest());

            // Assert
            Assert.True(result.Status == Models.PaymentStatus.Rejected);
        }

        [Fact]
        public async Task SendPayment_WhenBankReturns503_ReturnsFailure()
        {
            // Arrange
            var bankClient = new Mock<IAcquiringBankClient>();

            var httpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("")
            };

            bankClient
                .Setup(x => x.SendPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(httpResponse);

            var service = new AcquiringBank(
                bankClient.Object,
                Mock.Of<ILogger<AcquiringBank>>());

            // Act
            var result = await service.SendPayment(new PaymentRequest());

            // Assert
            Assert.True(result.Status == Models.PaymentStatus.Rejected);
        }

        [Fact]
        public async Task SendPayment_WhenResponseBodyCannotBeDeserialized_ReturnsFailure()
        {
            // Arrange
            var bankClient = new Mock<IAcquiringBankClient>();

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            };

            bankClient
                .Setup(x => x.SendPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(httpResponse);

            var service = new AcquiringBank(
                bankClient.Object,
                Mock.Of<ILogger<AcquiringBank>>());

            // Act
            var result = await service.SendPayment(new PaymentRequest());

            // Assert
            Assert.True(result.Status == Models.PaymentStatus.Rejected);
        }
    }
}
