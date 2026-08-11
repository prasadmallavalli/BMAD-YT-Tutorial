using OrderFlow.Domain;

namespace OrderFlow.Exhibits.Before.Dip;

// BEFORE: DIP violation. The concrete SqlCustomerRepository is instantiated inside the
// consumer, not passed in — there is no way to substitute anything else, so this class is
// untestable without a real database by construction. Compare to After/Dip.CustomerLookupService,
// which depends on ICustomerRepository via constructor injection instead.
public class CustomerLookupService
{
    private readonly SqlCustomerRepository _repository = new();

    public Customer? FindCustomer(int id)
    {
        var customer = _repository.FindById(id);
        Console.WriteLine(customer is not null
            ? $"[CustomerLookupService] Found: {customer.Name} <{customer.Email}>"
            : $"[CustomerLookupService] Customer {id} not found.");
        return customer;
    }
}
