using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

// Constructed only by UnitOfWork, never DI-registered independently (AD-9).
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    // .Include(o => o.OrderItems) is required — without it, Order.OrderItems comes back
    // empty (mirrors ProductRepository's .Include(p => p.Inventory), Story 1.4).
    public Task<Order?> GetByIdAsync(int id) =>
        _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Order>> GetAllAsync() =>
        await _context.Orders.Include(o => o.OrderItems).ToListAsync();

    // Passing an Order with its OrderItems collection already populated lets EF Core
    // cascade-insert both in one SaveChangesAsync call (AC #2) — no two-step insert.
    public async Task AddAsync(Order order) =>
        await _context.Orders.AddAsync(order);
}
