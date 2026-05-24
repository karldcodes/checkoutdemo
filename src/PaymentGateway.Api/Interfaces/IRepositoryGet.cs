namespace PaymentGateway.Api.Interfaces
{
    public interface IRepositoryGet<T, TKey>
    {
        Task<T?> Get(TKey id);
    }
}
