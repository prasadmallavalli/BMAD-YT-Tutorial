using OrderFlow.Domain;

namespace OrderFlow.DAL;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IReadOnlyList<Order>> GetAllAsync();
    Task AddAsync(Order order);
}
