using PaymentGateway.Api.Models.Merchant;

namespace PaymentGateway.Api.Interfaces;

public interface IPaymentsRepository: IRepositoryAdd<Payment>, IRepositoryGet<Payment, Guid>
{
}
