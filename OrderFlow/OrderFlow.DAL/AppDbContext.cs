using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

// Program.cs's IDbContextFactory<AppDbContext> registration (AD-2) compiles and passes
// ServiceProvider validation (ValidateOnBuild); actual runtime resolution on Windows
// remains UNVERIFIED-ENVIRONMENT — see Story 1.1 Task 5 / Completion Notes.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Internal per AD-9 ("DbSet<T> types remain fully internal") — only the sanctioned
    // AppDbContext type itself is public; the matching Repository (same assembly) is the
    // only caller.
    internal DbSet<Customer> Customers => Set<Customer>();
    internal DbSet<Product> Products => Set<Product>();
    internal DbSet<Inventory> Inventory => Set<Inventory>();
    internal DbSet<Order> Orders => Set<Order>();
    internal DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
    }

    public override int SaveChanges()
    {
        StampAuditableEntries();

        try
        {
            return base.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Same translation as UnitOfWork.SaveChangesAsync (AD-10) — kept in sync so the
            // sync path isn't a silent gap in the "one choke point" guarantee, even though
            // nothing calls it directly today.
            throw new ConcurrencyConflictException(ex);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditableEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    // AD-6: stamp UpdatedAt on every save; stamp CreatedAt only when the entity was just Added.
    // Never overwrite CreatedAt on Modified — repositories update via targeted property
    // changes on an already-tracked entity, never a blanket Update() (see Story 1.2 Dev Notes).
    private void StampAuditableEntries()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
