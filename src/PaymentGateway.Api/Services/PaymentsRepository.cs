using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Merchant;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository : IPaymentsRepository
{
    public List<Payment> Payments = new();
    
    public async Task Add(Payment payment)
    {
        Payments.Add(payment);
    }

    public async Task<Payment?> Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}