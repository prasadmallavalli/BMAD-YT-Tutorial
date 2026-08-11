using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation.Tests;

public class OrderCreatePresenterTests
{
    [Fact]
    public async Task LoadCustomersAsync_OnSuccess_DisplaysCustomers()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<ICustomerService>();
        var customers = new List<CustomerDto> { new() { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" } };
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IReadOnlyList<CustomerDto>>.Success(customers));

        var mockView = new Mock<IOrderCreateView>();
        var presenter = new OrderCreatePresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadCustomersAsync();

        mockView.Verify(v => v.DisplayCustomers(customers), Times.Once);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadCustomersAsync_OnFailure_ShowsError()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<ICustomerService>();
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IReadOnlyList<CustomerDto>>.Failure("boom"));

        var mockView = new Mock<IOrderCreateView>();
        var presenter = new OrderCreatePresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadCustomersAsync();

        mockView.Verify(v => v.ShowError("boom"), Times.Once);
        mockView.Verify(v => v.DisplayCustomers(It.IsAny<IReadOnlyList<CustomerDto>>()), Times.Never);
    }

    [Fact]
    public async Task LoadProductsAsync_OnSuccess_DisplaysProducts()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        var products = new List<ProductDto> { new() { Id = 1, Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m, StockQuantity = 10 } };
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IReadOnlyList<ProductDto>>.Success(products));

        var mockView = new Mock<IOrderCreateView>();
        var presenter = new OrderCreatePresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadProductsAsync();

        mockView.Verify(v => v.DisplayProducts(products), Times.Once);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadProductsAsync_OnFailure_ShowsError()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IReadOnlyList<ProductDto>>.Failure("boom"));

        var mockView = new Mock<IOrderCreateView>();
        var presenter = new OrderCreatePresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadProductsAsync();

        mockView.Verify(v => v.ShowError("boom"), Times.Once);
        mockView.Verify(v => v.DisplayProducts(It.IsAny<IReadOnlyList<ProductDto>>()), Times.Never);
    }

    // OrderProcessorFactory is a concrete class with a non-virtual Create method, so it can't
    // be mocked directly with Moq. Instead, wire a real OrderProcessorFactory inside the mocked
    // scope's provider, backed by a keyed-DI registration that resolves a mocked IOrderProcessor
    // — mirrors OrderProcessorFactoryTests.BuildFactory()'s DI-composition style.
    private static (Mock<IServiceScopeFactory> scopeFactory, Mock<IOrderProcessor> processor) CreateOrderProcessorScope(OrderType orderType)
    {
        var mockProcessor = new Mock<IOrderProcessor>();

        var services = new ServiceCollection();
        services.AddKeyedScoped<IOrderProcessor>(orderType, (_, _) => mockProcessor.Object);
        services.AddScoped<OrderProcessorFactory>();
        var provider = services.BuildServiceProvider();

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(provider);
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        return (mockScopeFactory, mockProcessor);
    }

    [Fact]
    public async Task ConfirmAsync_OnSuccess_ReturnsTrue()
    {
        var (scopeFactory, processor) = CreateOrderProcessorScope(OrderType.Standard);
        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            OrderType = OrderType.Standard,
            Items = [new OrderItemDto { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m }]
        };
        processor.Setup(p => p.ConfirmAsync(request))
            .ReturnsAsync(Result<OrderDto>.Success(new OrderDto { Id = 1, CustomerId = 1, OrderType = OrderType.Standard }));

        var mockView = new Mock<IOrderCreateView>();
        var presenter = new OrderCreatePresenter(mockView.Object, scopeFactory.Object);

        var confirmed = await presenter.ConfirmAsync(request);

        Assert.True(confirmed);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_OnFailure_ShowsErrorAndReturnsFalse()
    {
        var (scopeFactory, processor) = CreateOrderProcessorScope(OrderType.Standard);
        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            OrderType = OrderType.Standard,
            Items = [new OrderItemDto { ProductId = 3, Quantity = 100, UnitPriceAtOrder = 9.99m }]
        };
        processor.Setup(p => p.ConfirmAsync(request))
            .ReturnsAsync(Result<OrderDto>.Failure("Insufficient stock for product(s): 3"));

        var mockView = new Mock<IOrderCreateView>();
        var presenter = new OrderCreatePresenter(mockView.Object, scopeFactory.Object);

        var confirmed = await presenter.ConfirmAsync(request);

        Assert.False(confirmed);
        mockView.Verify(v => v.ShowError("Insufficient stock for product(s): 3"), Times.Once);
    }
}
