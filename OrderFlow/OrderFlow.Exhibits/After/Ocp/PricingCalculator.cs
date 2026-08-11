using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Ocp;

// AFTER: OCP refactor of Before/Ocp.PricingCalculator via the Strategy pattern — mirrors
// AD-11's real IPricingStrategy shape. Adding a new discount type is a new IDiscountStrategy
// implementation; zero changes to this class or any existing strategy.
public class PricingCalculator
{
    public decimal CalculateTotal(IEnumerable<OrderItem> items, IDiscountStrategy discountStrategy)
    {
        var baseTotal = items.Sum(i => i.Quantity * i.UnitPriceAtOrder);
        var total = discountStrategy.Apply(baseTotal);
        return total < 0 ? 0 : total;
    }
}
