using OrderFlow.Domain;

namespace OrderFlow.DAL;

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(int productId);
}
