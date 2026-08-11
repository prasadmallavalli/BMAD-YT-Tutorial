using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Srp;

// AFTER: this class's only reason to change is the persistence mechanism.
public class OrderPersister
{
    private readonly List<Order> _persistedOrders = [];

    public void Save(Order order) => _persistedOrders.Add(order);

    public int Count => _persistedOrders.Count;
}
