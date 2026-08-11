using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderFlow.BLL;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class OrderProcessorFactoryTests
{
    private static OrderProcessorFactory BuildFactory()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPricingStrategy, StandardPricingStrategy>();
        // StandardOrderProcessor/RushOrderProcessor now also need IUnitOfWork/IInventoryService/
        // IOrderStatusService (Story 2.5) — these tests only assert type resolution, so
        // unconfigured mocks are sufficient, no Setup calls needed.
        services.AddScoped(_ => Mock.Of<IUnitOfWork>());
        services.AddScoped(_ => Mock.Of<IInventoryService>());
        services.AddScoped(_ => Mock.Of<IOrderStatusService>());
        services.AddKeyedScoped<IOrderProcessor, StandardOrderProcessor>(OrderType.Standard);
        services.AddKeyedScoped<IOrderProcessor, RushOrderProcessor>(OrderType.Rush);
        services.AddScoped<OrderProcessorFactory>();

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<OrderProcessorFactory>();
    }

    [Fact]
    public void Create_WithStandardOrderType_ReturnsStandardOrderProcessor()
    {
        var factory = BuildFactory();

        var processor = factory.Create(OrderType.Standard);

        Assert.IsType<StandardOrderProcessor>(processor);
    }

    [Fact]
    public void Create_WithRushOrderType_ReturnsRushOrderProcessor()
    {
        var factory = BuildFactory();

        var processor = factory.Create(OrderType.Rush);

        Assert.IsType<RushOrderProcessor>(processor);
    }

    [Fact]
    public void Create_WithUnmappedOrderType_Throws()
    {
        var factory = BuildFactory();

        // Locks down OrderProcessorFactory.Create's documented throwing contract for an
        // OrderType with no keyed registration — see its doc comment.
        Assert.Throws<InvalidOperationException>(() => factory.Create(OrderType.Unspecified));
    }
}
