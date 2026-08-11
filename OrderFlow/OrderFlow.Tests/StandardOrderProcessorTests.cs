using Moq;
using OrderFlow.BLL;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class StandardOrderProcessorTests
{
    // EF only assigns an identity Id on save — a plain mock leaves order.Id == 0 unless
    // AddAsync's callback assigns it, mirroring what a real SaveChangesAsync insert would do.
    private static (Mock<IUnitOfWork> unitOfWork, Mock<IOrderRepository> orderRepository,
        Mock<IInventoryRepository> inventoryRepository, Mock<IInventoryService> inventoryService,
        Mock<IOrderStatusService> orderStatusService) BuildMocks()
    {
        var orderRepository = new Mock<IOrderRepository>();
        orderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = 42)
            .Returns(Task.CompletedTask);

        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository.Setup(r => r.GetByProductIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int productId) => new Inventory { ProductId = productId, StockQuantity = 100 });

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Orders).Returns(orderRepository.Object);
        unitOfWork.Setup(u => u.Inventory).Returns(inventoryRepository.Object);
        unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var inventoryService = new Mock<IInventoryService>();
        inventoryService.Setup(s => s.HasSufficientStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var orderStatusService = new Mock<IOrderStatusService>();
        orderStatusService.Setup(s => s.TransitionTo(It.IsAny<int>(), OrderStatus.Confirmed))
            .ReturnsAsync(Result<OrderStatus>.Success(OrderStatus.Confirmed));

        return (unitOfWork, orderRepository, inventoryRepository, inventoryService, orderStatusService);
    }

    [Fact]
    public async Task ConfirmAsync_ReturnsBaseTotalUnmodifiedAndConfirmsOrder()
    {
        var (unitOfWork, orderRepository, _, inventoryService, orderStatusService) = BuildMocks();
        var pricingStrategy = new StandardPricingStrategy();
        var processor = new StandardOrderProcessor(
            pricingStrategy, unitOfWork.Object, inventoryService.Object, orderStatusService.Object);
        var items = new List<OrderItemDto>
        {
            new() { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m },
            new() { ProductId = 2, Quantity = 1, UnitPriceAtOrder = 19.99m }
        };
        var request = new CreateOrderRequest { CustomerId = 1, OrderType = OrderType.Standard, Items = items };
        var expectedTotal = pricingStrategy.CalculateTotal(items);

        var result = await processor.ConfirmAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedTotal, result.Value!.Total);
        Assert.Equal(request.CustomerId, result.Value.CustomerId);
        Assert.Equal(OrderType.Standard, result.Value.OrderType);
        Assert.Equal(42, result.Value.Id);
        Assert.Equal(OrderStatus.Confirmed, result.Value.Status);
        orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        orderStatusService.Verify(s => s.TransitionTo(42, OrderStatus.Confirmed), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_WithInsufficientStock_ReturnsFailureAndPersistsNothing()
    {
        var (unitOfWork, orderRepository, _, inventoryService, orderStatusService) = BuildMocks();
        inventoryService.Setup(s => s.HasSufficientStockAsync(2, It.IsAny<int>()))
            .ReturnsAsync(Result<bool>.Success(false));
        var pricingStrategy = new StandardPricingStrategy();
        var processor = new StandardOrderProcessor(
            pricingStrategy, unitOfWork.Object, inventoryService.Object, orderStatusService.Object);
        var items = new List<OrderItemDto>
        {
            new() { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m },
            new() { ProductId = 2, Quantity = 100, UnitPriceAtOrder = 19.99m }
        };
        var request = new CreateOrderRequest { CustomerId = 1, OrderType = OrderType.Standard, Items = items };

        var result = await processor.ConfirmAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Insufficient stock for product(s): 2", result.Error);
        orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        orderStatusService.Verify(s => s.TransitionTo(It.IsAny<int>(), It.IsAny<OrderStatus>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_OnInventoryConcurrencyConflict_ReturnsFriendlyFailure()
    {
        var (unitOfWork, orderRepository, _, inventoryService, orderStatusService) = BuildMocks();
        unitOfWork.SetupSequence(u => u.SaveChangesAsync())
            .ReturnsAsync(1)
            .ThrowsAsync(new ConcurrencyConflictException(new Exception()));
        var pricingStrategy = new StandardPricingStrategy();
        var processor = new StandardOrderProcessor(
            pricingStrategy, unitOfWork.Object, inventoryService.Object, orderStatusService.Object);
        var items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m } };
        var request = new CreateOrderRequest { CustomerId = 1, OrderType = OrderType.Standard, Items = items };

        var result = await processor.ConfirmAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ConcurrencyConflictException.DefaultMessage, result.Error);
        orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        orderStatusService.Verify(s => s.TransitionTo(It.IsAny<int>(), It.IsAny<OrderStatus>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_WithDuplicateProductLineItems_ChecksCombinedQuantityAndPersistsNothingWhenInsufficient()
    {
        var (unitOfWork, orderRepository, _, inventoryService, orderStatusService) = BuildMocks();
        // Two lines for the same Product (3 + 3 = 6) — each individually would pass a
        // per-line check against a stock of 5, but combined demand exceeds it.
        inventoryService.Setup(s => s.HasSufficientStockAsync(1, 6)).ReturnsAsync(Result<bool>.Success(false));
        var pricingStrategy = new StandardPricingStrategy();
        var processor = new StandardOrderProcessor(
            pricingStrategy, unitOfWork.Object, inventoryService.Object, orderStatusService.Object);
        var items = new List<OrderItemDto>
        {
            new() { ProductId = 1, Quantity = 3, UnitPriceAtOrder = 9.99m },
            new() { ProductId = 1, Quantity = 3, UnitPriceAtOrder = 9.99m }
        };
        var request = new CreateOrderRequest { CustomerId = 1, OrderType = OrderType.Standard, Items = items };

        var result = await processor.ConfirmAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Insufficient stock for product(s): 1", result.Error);
        inventoryService.Verify(s => s.HasSufficientStockAsync(1, 6), Times.Once);
        inventoryService.Verify(s => s.HasSufficientStockAsync(1, 3), Times.Never);
        orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_WhenInventoryInsufficientAtDecrementTime_ReturnsFailureWithoutThrowing()
    {
        var (unitOfWork, orderRepository, inventoryRepository, inventoryService, orderStatusService) = BuildMocks();
        // Step 1's check (mocked) says sufficient, but the fresh read at Step 4 (e.g. a
        // near-simultaneous confirm on the same product already committed) shows otherwise.
        inventoryRepository.Setup(r => r.GetByProductIdAsync(1))
            .ReturnsAsync(new Inventory { ProductId = 1, StockQuantity = 1 });
        var pricingStrategy = new StandardPricingStrategy();
        var processor = new StandardOrderProcessor(
            pricingStrategy, unitOfWork.Object, inventoryService.Object, orderStatusService.Object);
        var items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m } };
        var request = new CreateOrderRequest { CustomerId = 1, OrderType = OrderType.Standard, Items = items };

        var result = await processor.ConfirmAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Insufficient stock for product(s): 1", result.Error);
        orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        orderStatusService.Verify(s => s.TransitionTo(It.IsAny<int>(), It.IsAny<OrderStatus>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_WhenInventoryRowMissingAtDecrementTime_ReturnsFailureWithoutThrowing()
    {
        var (unitOfWork, orderRepository, inventoryRepository, inventoryService, orderStatusService) = BuildMocks();
        inventoryRepository.Setup(r => r.GetByProductIdAsync(1)).ReturnsAsync((Inventory?)null);
        var pricingStrategy = new StandardPricingStrategy();
        var processor = new StandardOrderProcessor(
            pricingStrategy, unitOfWork.Object, inventoryService.Object, orderStatusService.Object);
        var items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m } };
        var request = new CreateOrderRequest { CustomerId = 1, OrderType = OrderType.Standard, Items = items };

        var result = await processor.ConfirmAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Insufficient stock for product(s): 1", result.Error);
        orderStatusService.Verify(s => s.TransitionTo(It.IsAny<int>(), It.IsAny<OrderStatus>()), Times.Never);
    }
}
