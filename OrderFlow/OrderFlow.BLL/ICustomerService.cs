namespace OrderFlow.BLL;

public interface ICustomerService
{
    Task<Result<CustomerDto>> CreateAsync(CustomerDto dto);
    Task<Result<CustomerDto>> GetAsync(int id);
    Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync();
    Task<Result<CustomerDto>> UpdateAsync(int id, CustomerDto dto);
}
