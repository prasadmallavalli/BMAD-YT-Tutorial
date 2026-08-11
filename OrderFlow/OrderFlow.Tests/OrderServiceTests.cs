using Moq;
using OrderFlow.BLL;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task GetAsync_WithExistingOrder_ReturnsDtoWithCustomerNameAndProductNamesAndComputedTotal()
    {
        var order = new Order
        {
            Id = 1,
            CustomerId = 10,
            OrderType = OrderType.Standard,
            Status = OrderStatus.Confirmed,
            OrderItems = [new OrderItem { ProductId = 100, Quantity = 2, UnitPriceAtOrder = 9.99m }]
        };
        var customer = new Customer { Id = 10, Name = "Ada Lovelace", Email = "ada@example.com" };
        var product = new Product { Id = 100, Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m };

        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockCustomerRepository = new Mock<ICustomerRepository>();
        mockCustomerRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(customer);
        var mockProductRepository = new Mock<IProductRepository>();
        mockProductRepository.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(product);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepository.Object);
        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var service = new OrderService(mockUnitOfWork.Object, new StandardPricingStrategy());

        var result = await service.GetAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ada Lovelace", result.Value!.CustomerName);
        Assert.Single(result.Value.Items);
        Assert.Equal("Widget", result.Value.Items[0].ProductName);
        Assert.Equal(9.99m * 2, result.Value.Total);
        // Only the products this order references are fetched — not the whole catalog
        // (Review Findings patch: was previously Products.GetAllAsync()).
        mockProductRepository.Verify(r => r.GetByIdAsync(100), Times.Once);
        mockProductRepository.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WithMissingOrder_ReturnsFailure()
    {
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);

        var service = new OrderService(mockUnitOfWork.Object, new StandardPricingStrategy());

        var result = await service.GetAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal("Order not found", result.Error);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrdersWithNamesAndTotals_UsingBatchLookupsNotPerOrderQueries()
    {
        var orders = new List<Order>
        {
            new()
            {
                Id = 1, CustomerId = 10, OrderType = OrderType.Standard, Status = OrderStatus.Confirmed,
                OrderItems = [new OrderItem { ProductId = 100, Quantity = 1, UnitPriceAtOrder = 9.99m }]
            },
            new()
            {
                Id = 2, CustomerId = 20, OrderType = OrderType.Rush, Status = OrderStatus.Confirmed,
                OrderItems = [new OrderItem { ProductId = 100, Quantity = 1, UnitPriceAtOrder = 9.99m }]
            }
        };
        var customers = new List<Customer>
        {
            new() { Id = 10, Name = "Ada Lovelace", Email = "ada@example.com" },
            new() { Id = 20, Name = "Grace Hopper", Email = "grace@example.com" }
        };
        var products = new List<Product> { new() { Id = 100, Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m } };

        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(orders);
        var mockCustomerRepository = new Mock<ICustomerRepository>();
        mockCustomerRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(customers);
        var mockProductRepository = new Mock<IProductRepository>();
        mockProductRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepository.Object);
        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var service = new OrderService(mockUnitOfWork.Object, new StandardPricingStrategy());

        var result = await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("Ada Lovelace", result.Value[0].CustomerName);
        Assert.Equal("Grace Hopper", result.Value[1].CustomerName);
        Assert.Equal("Widget", result.Value[0].Items[0].ProductName);
        // Rush order's total includes the 10% surcharge; Standard's doesn't — proves per-Order
        // OrderType is respected during the batch mapping pass.
        Assert.Equal(9.99m, result.Value[0].Total);
        Assert.Equal(Math.Round(9.99m * 1.10m, 2, MidpointRounding.AwayFromZero), result.Value[1].Total);
        mockCustomerRepository.Verify(r => r.GetAllAsync(), Times.Once);
        mockProductRepository.Verify(r => r.GetAllAsync(), Times.Once);
        mockCustomerRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
}
