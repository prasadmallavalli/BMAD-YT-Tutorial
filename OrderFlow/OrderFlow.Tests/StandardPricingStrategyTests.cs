using OrderFlow.BLL;

namespace OrderFlow.Tests;

public class StandardPricingStrategyTests
{
    [Fact]
    public void CalculateTotal_WithMultipleLineItems_SumsQuantityTimesUnitPrice()
    {
        var strategy = new StandardPricingStrategy();
        var items = new[]
        {
            new OrderItemDto { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m },
            new OrderItemDto { ProductId = 2, Quantity = 1, UnitPriceAtOrder = 19.99m },
            new OrderItemDto { ProductId = 3, Quantity = 3, UnitPriceAtOrder = 4.50m }
        };

        var total = strategy.CalculateTotal(items);

        // Independently-computed literal (19.98 + 19.99 + 13.50), not a mirror of the
        // production expression — a swapped-operand or off-by-one bug would still be caught.
        Assert.Equal(53.47m, total);
    }

    [Fact]
    public void CalculateTotal_WithSingleLineItem_ReturnsQuantityTimesUnitPrice()
    {
        var strategy = new StandardPricingStrategy();
        var items = new[] { new OrderItemDto { ProductId = 1, Quantity = 5, UnitPriceAtOrder = 3.20m } };

        var total = strategy.CalculateTotal(items);

        Assert.Equal(16.00m, total);
    }

    [Fact]
    public void CalculateTotal_WithEmptyCollection_ReturnsZero()
    {
        var strategy = new StandardPricingStrategy();

        var total = strategy.CalculateTotal([]);

        Assert.Equal(0m, total);
    }
}
