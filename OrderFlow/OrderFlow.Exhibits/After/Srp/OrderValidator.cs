using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Srp;

// AFTER: one of three single-responsibility collaborators (see also OrderPersister,
// OrderNotifier) OrderProcessor composes — this class's only reason to change is a
// validation rule. Error text matches Before/Srp.OrderProcessor's inline checks exactly,
// so both exhibits produce equivalent output for the same input orders.
public class OrderValidator
{
    public bool Validate(Order order, out string? error)
    {
        if (order.OrderItems.Count == 0)
        {
            error = "no line items.";
            return false;
        }

        foreach (var item in order.OrderItems)
        {
            if (item.Quantity <= 0)
            {
                error = $"item {item.ProductId} has non-positive quantity.";
                return false;
            }
        }

        error = null;
        return true;
    }
}
