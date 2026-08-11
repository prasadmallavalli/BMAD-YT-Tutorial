namespace OrderFlow.DAL;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    ICustomerRepository Customers { get; }
    IProductRepository Products { get; }
    IInventoryRepository Inventory { get; }
    IOrderRepository Orders { get; }

    Task<int> SaveChangesAsync();
}
