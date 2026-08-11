using Microsoft.EntityFrameworkCore;

namespace OrderFlow.DAL;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    // AD-2: only UnitOfWork touches IDbContextFactory<AppDbContext>, once, at construction.
    // Uses the synchronous CreateDbContext() overload — DI constructors can't await, and
    // this is a genuine sync EF Core API, not a blocked-on-Task antipattern (see Story 1.2
    // Dev Notes).
    public UnitOfWork(IDbContextFactory<AppDbContext> contextFactory)
    {
        _context = contextFactory.CreateDbContext();
        Customers = new CustomerRepository(_context);
        Products = new ProductRepository(_context);
        Inventory = new InventoryRepository(_context);
        Orders = new OrderRepository(_context);
    }

    public ICustomerRepository Customers { get; }
    public IProductRepository Products { get; }
    public IInventoryRepository Inventory { get; }
    public IOrderRepository Orders { get; }

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // AD-10: translate the EF Core-specific exception into a plain DAL exception
            // BLL can catch without depending on EF Core directly (AD-9). One choke point —
            // every future RowVersion-carrying entity inherits this for free.
            throw new ConcurrencyConflictException(ex);
        }
    }

    public void Dispose() => _context.Dispose();

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
