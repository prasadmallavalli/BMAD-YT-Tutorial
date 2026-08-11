using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Dip;

// AFTER: DIP refactor of Before/Dip.CustomerLookupService. Depends on the ICustomerRepository
// abstraction via constructor injection — never instantiates a concrete implementation itself.
public class CustomerLookupService
{
    private readonly ICustomerRepository _repository;

    public CustomerLookupService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public Customer? FindCustomer(int id)
    {
        var customer = _repository.FindById(id);
        Console.WriteLine(customer is not null
            ? $"[CustomerLookupService] Found: {customer.Name} <{customer.Email}>"
            : $"[CustomerLookupService] Customer {id} not found.");
        return customer;
    }
}
