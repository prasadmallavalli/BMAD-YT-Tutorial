using Microsoft.EntityFrameworkCore;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class OrderRepositoryTests
{
    // Fresh unique database name per test — the InMemory provider does not reset between
    // tests otherwise.
    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

    // Seeds a real Customer + 2 Products so the Order/OrderItems below reference rows that
    // actually exist — the InMemory provider doesn't enforce FK referential integrity, but a
    // real relational provider would, and this keeps the round-trip test's fixture faithful.
    private static async Task<(int customerId, int productId1, int productId2)> SeedCustomerAndProductsAsync(AppDbContext context)
    {
        // AppDbContext's DbSet<T> properties are internal (AD-9) — seed through the public
        // repositories, same as production code would, rather than reaching for the DbSets.
        var customer = new Customer { Name = "Ada Lovelace", Email = "ada@example.com" };
        var product1 = new Product { Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m };
        var product2 = new Product { Name = "Gadget", SKU = "GAD-001", UnitPrice = 19.99m };

        await new CustomerRepository(context).AddAsync(customer);
        await new ProductRepository(context).AddAsync(product1);
        await new ProductRepository(context).AddAsync(product2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (customer.Id, product1.Id, product2.Id);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsOrderWithOrderItems()
    {
        var dbName = Guid.NewGuid().ToString();
        int orderId;
        int customerId, productId1, productId2;

        await using (var writeContext = new AppDbContext(CreateOptions(dbName)))
        {
            (customerId, productId1, productId2) = await SeedCustomerAndProductsAsync(writeContext);

            var order = new Order
            {
                CustomerId = customerId,
                OrderType = OrderType.Standard,
                Status = OrderStatus.Confirmed,
                OrderItems =
                {
                    new OrderItem { ProductId = productId1, Quantity = 2, UnitPriceAtOrder = 9.99m },
                    new OrderItem { ProductId = productId2, Quantity = 1, UnitPriceAtOrder = 19.99m }
                }
            };

            var writeRepository = new OrderRepository(writeContext);
            await writeRepository.AddAsync(order);
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            orderId = order.Id;
        }

        // Second AppDbContext instance, same database name — proves the data actually
        // persisted, not just that it's sitting in the first context's change tracker.
        await using var readContext = new AppDbContext(CreateOptions(dbName));
        var readRepository = new OrderRepository(readContext);
        var retrieved = await readRepository.GetByIdAsync(orderId);

        Assert.NotNull(retrieved);
        Assert.Equal(customerId, retrieved!.CustomerId);
        Assert.Equal(OrderType.Standard, retrieved.OrderType);
        Assert.Equal(OrderStatus.Confirmed, retrieved.Status);
        Assert.NotEqual(default, retrieved.CreatedAt);
        Assert.NotEqual(default, retrieved.UpdatedAt);
        Assert.Equal(2, retrieved.OrderItems.Count);
        Assert.Contains(retrieved.OrderItems, oi => oi.ProductId == productId1 && oi.Quantity == 2 && oi.UnitPriceAtOrder == 9.99m && oi.CreatedAt != default);
        Assert.Contains(retrieved.OrderItems, oi => oi.ProductId == productId2 && oi.Quantity == 1 && oi.UnitPriceAtOrder == 19.99m && oi.CreatedAt != default);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonexistentId_ReturnsNull()
    {
        await using var context = new AppDbContext(CreateOptions(Guid.NewGuid().ToString()));
        var repository = new OrderRepository(context);

        var result = await repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrdersWithOrderItems()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var writeContext = new AppDbContext(CreateOptions(dbName)))
        {
            var (customerId, productId1, _) = await SeedCustomerAndProductsAsync(writeContext);
            var writeRepository = new OrderRepository(writeContext);

            await writeRepository.AddAsync(new Order
            {
                CustomerId = customerId,
                OrderType = OrderType.Standard,
                Status = OrderStatus.Confirmed,
                OrderItems = { new OrderItem { ProductId = productId1, Quantity = 1, UnitPriceAtOrder = 9.99m } }
            });
            await writeRepository.AddAsync(new Order
            {
                CustomerId = customerId,
                OrderType = OrderType.Rush,
                Status = OrderStatus.Confirmed,
                OrderItems = { new OrderItem { ProductId = productId1, Quantity = 3, UnitPriceAtOrder = 9.99m } }
            });
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = new AppDbContext(CreateOptions(dbName));
        var readRepository = new OrderRepository(readContext);

        var allOrders = await readRepository.GetAllAsync();

        Assert.Equal(2, allOrders.Count);
        Assert.All(allOrders, o => Assert.Single(o.OrderItems));
    }
}
