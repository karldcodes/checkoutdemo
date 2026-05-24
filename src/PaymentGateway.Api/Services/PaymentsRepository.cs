using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Merchant;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly List<Payment> _payments = [];
    
    public async Task Add(Payment payment)
    {
        _payments.Add(payment);
    }

    public async Task<Payment?> Get(Guid id)
    {
        return _payments.FirstOrDefault(p => p.Id == id);
    }
}