using OrderFlow.Domain;

namespace OrderFlow.Exhibits.Before.Dip;

// Stands in for a real ADO.NET/EF Core data-access class talking to an actual database.
// Deliberately has no interface — Before/Dip.CustomerLookupService couples directly to this
// concrete class.
public class SqlCustomerRepository
{
    public Customer? FindById(int id)
    {
        Console.WriteLine($"[SqlCustomerRepository] Querying real database for Customer {id}...");
        return id == 1 ? new Customer { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" } : null;
    }
}
