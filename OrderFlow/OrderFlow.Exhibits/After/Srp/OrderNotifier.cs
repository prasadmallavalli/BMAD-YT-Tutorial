using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Srp;

// AFTER: this class's only reason to change is the notification channel.
public class OrderNotifier
{
    public void Notify(Order order) =>
        Console.WriteLine($"[Notification] Order {order.Id} confirmed for customer {order.CustomerId}.");
}
