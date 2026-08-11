using OrderFlow.Domain;

namespace OrderFlow.Exhibits.Before.Ocp;

// BEFORE: OCP violation. Adding a fourth DiscountType requires adding a case here — this
// class is never "closed for modification." Compare to After/Ocp.PricingCalculator, which
// adds new discount behavior via a new IDiscountStrategy implementation instead.
public class PricingCalculator
{
    public decimal CalculateTotal(IEnumerable<OrderItem> items, DiscountType discountType, decimal discountValue)
    {
        // Mirrors OrderFlow.BLL.StandardPricingStrategy's real formula (Story 2.2), but over
        // the Domain OrderItem directly — Exhibits cannot reference OrderFlow.BLL's
        // OrderItemDto (AD-8's only permitted reference is OrderFlow.Domain).
        var baseTotal = items.Sum(i => i.Quantity * i.UnitPriceAtOrder);

        decimal total;
        switch (discountType)
        {
            case DiscountType.None:
                total = baseTotal;
                break;
            case DiscountType.Percentage:
                total = baseTotal - (baseTotal * discountValue / 100m);
                break;
            case DiscountType.FlatAmount:
                total = baseTotal - discountValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(discountType));
        }

        return total < 0 ? 0 : total;
    }
}
