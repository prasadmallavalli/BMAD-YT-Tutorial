namespace OrderFlow.BLL;

// AD-11: the sole active IPricingStrategy, registered Scoped at the composition root with no
// keyed dispatch. Sums Quantity x UnitPriceAtOrder — no discount (Epic 2's own scope decision).
public class StandardPricingStrategy : IPricingStrategy
{
    public decimal CalculateTotal(IEnumerable<OrderItemDto> items) =>
        items.Sum(item => item.Quantity * item.UnitPriceAtOrder);
}
