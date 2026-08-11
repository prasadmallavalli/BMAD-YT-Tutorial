using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Srp;

// AFTER: SRP refactor of Before/Srp.OrderProcessor. Composes three single-responsibility
// collaborators (constructor injection — plain `new`-ed by AfterSrpRunner, no DI container;
// see Program.cs) instead of doing all three jobs inline. Produces the exact same
// [Validation]/[Persistence]/[Notification] output as the Before version for the same inputs.
public class OrderProcessor
{
    private readonly OrderValidator _validator;
    private readonly OrderPersister _persister;
    private readonly OrderNotifier _notifier;

    public OrderProcessor(OrderValidator validator, OrderPersister persister, OrderNotifier notifier)
    {
        _validator = validator;
        _persister = persister;
        _notifier = notifier;
    }

    public bool Process(Order order)
    {
        if (!_validator.Validate(order, out var error))
        {
            Console.WriteLine($"[Validation] Order {order.Id} rejected: {error}");
            return false;
        }

        _persister.Save(order);
        Console.WriteLine($"[Persistence] Order {order.Id} saved. Total persisted: {_persister.Count}.");

        _notifier.Notify(order);

        return true;
    }
}
