using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class OrderTotalCalculatorTests
{
    [Fact]
    public void Calculate_Standard_ReturnsBaseTotalUnmodified()
    {
        var pricingStrategy = new StandardPricingStrategy();
        var items = new List<OrderItemDto>
        {
            new() { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m },
            new() { ProductId = 2, Quantity = 1, UnitPriceAtOrder = 19.99m }
        };
        var expectedTotal = pricingStrategy.CalculateTotal(items);

        var total = OrderTotalCalculator.Calculate(OrderType.Standard, pricingStrategy, items);

        Assert.Equal(expectedTotal, total);
    }

    [Fact]
    public void Calculate_Rush_AppliesTenPercentSurchargeRounded()
    {
        var pricingStrategy = new StandardPricingStrategy();
        var items = new List<OrderItemDto>
        {
            new() { ProductId = 1, Quantity = 2, UnitPriceAtOrder = 9.99m },
            new() { ProductId = 2, Quantity = 1, UnitPriceAtOrder = 19.99m }
        };

        // Independently-computed expected value, not a mirror of the production expression's
        // own structure — matches RushOrderProcessorTests' established pattern.
        var baseTotal = pricingStrategy.CalculateTotal(items);
        var expectedTotal = Math.Round(baseTotal * 1.10m, 2, MidpointRounding.AwayFromZero);

        var total = OrderTotalCalculator.Calculate(OrderType.Rush, pricingStrategy, items);

        Assert.Equal(expectedTotal, total);
    }
}
