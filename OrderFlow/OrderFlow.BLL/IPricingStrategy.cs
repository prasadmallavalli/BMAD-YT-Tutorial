namespace OrderFlow.BLL;

public interface IPricingStrategy
{
    // Assumes items is non-null and already validated (non-negative Quantity/UnitPriceAtOrder)
    // by the caller — this is a pure calculation, not a validation boundary. Line-item
    // validation is the order-creation flow's responsibility (Story 2.5), not this interface's.
    decimal CalculateTotal(IEnumerable<OrderItemDto> items);
}
