using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Dip;

// A fake ICustomerRepository with no real database at all — this is what Before/Dip's design
// makes impossible. AfterDipRunner substitutes this for SqlCustomerRepository without
// touching CustomerLookupService (AC #3).
public class FakeCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<int, Customer> _customers = new()
    {
        [1] = new Customer { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" }
    };

    public Customer? FindById(int id)
    {
        Console.WriteLine($"[FakeCustomerRepository] Looking up in-memory fixture for Customer {id}...");
        return _customers.GetValueOrDefault(id);
    }
}
