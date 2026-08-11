using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

// Constructed only by UnitOfWork, never DI-registered independently (AD-9).
public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    // Intentionally tracked (no AsNoTracking) — CustomerService.UpdateAsync relies on this
    // entity staying attached to the shared per-operation DbContext so direct property
    // mutation is picked up by EF's change tracker (see Story 1.2 Dev Notes, AD-6).
    public Task<Customer?> GetByIdAsync(int id) =>
        _context.Customers.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Customer>> GetAllAsync() =>
        await _context.Customers.ToListAsync();

    public async Task AddAsync(Customer customer) =>
        await _context.Customers.AddAsync(customer);
}
