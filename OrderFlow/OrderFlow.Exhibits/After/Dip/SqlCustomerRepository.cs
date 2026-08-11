using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Dip;

// The same "real database" behavior as Before/Dip.SqlCustomerRepository, now behind
// ICustomerRepository so CustomerLookupService can depend on the abstraction instead.
public class SqlCustomerRepository : ICustomerRepository
{
    public Customer? FindById(int id)
    {
        Console.WriteLine($"[SqlCustomerRepository] Querying real database for Customer {id}...");
        return id == 1 ? new Customer { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" } : null;
    }
}
