using OrderFlow.Domain;

namespace OrderFlow.Exhibits.Before.Srp;

// BEFORE: SRP violation. One class owns validation, persistence, AND notification — three
// unrelated reasons to change (a new validation rule, a new persistence mechanism, or a new
// notification channel all require editing this same class). Compare to After/Srp, which
// splits these into OrderValidator/OrderPersister/OrderNotifier for the same behavior.
public class OrderProcessor
{
    private readonly List<Order> _persistedOrders = [];

    public bool Process(Order order)
    {
        // Reason to change #1: validation rules.
        if (order.OrderItems.Count == 0)
        {
            Console.WriteLine($"[Validation] Order {order.Id} rejected: no line items.");
            return false;
        }

        foreach (var item in order.OrderItems)
        {
            if (item.Quantity <= 0)
            {
                Console.WriteLine($"[Validation] Order {order.Id} rejected: item {item.ProductId} has non-positive quantity.");
                return false;
            }
        }

        // Reason to change #2: persistence mechanism.
        _persistedOrders.Add(order);
        Console.WriteLine($"[Persistence] Order {order.Id} saved. Total persisted: {_persistedOrders.Count}.");

        // Reason to change #3: notification mechanism.
        Console.WriteLine($"[Notification] Order {order.Id} confirmed for customer {order.CustomerId}.");

        return true;
    }
}
