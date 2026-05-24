namespace PaymentGateway.Api.Interfaces
{
    public interface IRepositoryAdd<T>
    {
        Task Add(T item);
    }
}
