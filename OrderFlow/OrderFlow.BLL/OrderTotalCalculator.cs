using OrderFlow.Domain;

namespace OrderFlow.BLL;

// Shared by StandardOrderProcessor/RushOrderProcessor (order confirmation) and OrderService
// (order display, Story 3.2) — extracted once a third call site needed the identical "base
// total, +10% if Rush" calculation, closing Story 2.5's own deferred duplication finding.
// Public, not internal — matches every other BLL type's accessibility (Result, IPricingStrategy,
// etc.); this codebase has no InternalsVisibleTo plumbing for internal-type unit testing.
public static class OrderTotalCalculator
{
    private const decimal RushSurchargeRate = 0.10m;

    public static decimal Calculate(OrderType orderType, IPricingStrategy pricingStrategy, IEnumerable<OrderItemDto> items)
    {
        var baseTotal = pricingStrategy.CalculateTotal(items);

        if (orderType != OrderType.Rush)
        {
            return baseTotal;
        }

        // Multiplying by a 3-decimal rate can introduce sub-cent precision
        // (e.g. 39.97 * 1.10 = 43.967) — round to currency precision explicitly.
        return Math.Round(baseTotal + baseTotal * RushSurchargeRate, 2, MidpointRounding.AwayFromZero);
    }
}
