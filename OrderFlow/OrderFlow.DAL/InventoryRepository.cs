using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

// Constructed only by UnitOfWork, never DI-registered independently (AD-9).
public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;

    public InventoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Inventory?> GetByProductIdAsync(int productId) =>
        _context.Inventory.FirstOrDefaultAsync(i => i.ProductId == productId);
}
