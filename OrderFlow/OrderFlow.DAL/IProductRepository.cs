using OrderFlow.Domain;

namespace OrderFlow.DAL;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task AddAsync(Product product);
}
