using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

// Constructed only by UnitOfWork, never DI-registered independently (AD-9).
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    // .Include(p => p.Inventory) is required — without it, Product.Inventory comes back
    // null and ProductDto.StockQuantity would silently read as missing. Intentionally
    // tracked (no AsNoTracking) — ProductService.UpdateAsync mutates the tracked entity
    // (and its tracked Inventory navigation) directly (see Story 1.4 Dev Notes, AD-6).
    public Task<Product?> GetByIdAsync(int id) =>
        _context.Products.Include(p => p.Inventory).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _context.Products.Include(p => p.Inventory).ToListAsync();

    public async Task AddAsync(Product product) =>
        await _context.Products.AddAsync(product);
}
