using OrderFlow.Domain;

namespace OrderFlow.DAL;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task AddAsync(Customer customer);
}
