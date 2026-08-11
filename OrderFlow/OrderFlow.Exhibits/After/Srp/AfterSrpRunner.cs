using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Srp;

public static class AfterSrpRunner
{
    public static void Run()
    {
        Console.WriteLine("=== After: SRP Refactor ===");

        var processor = new OrderProcessor(new OrderValidator(), new OrderPersister(), new OrderNotifier());

        var validOrder = new Order
        {
            Id = 1,
            CustomerId = 100,
            OrderItems = [new OrderItem { ProductId = 10, Quantity = 2 }]
        };

        var invalidOrder = new Order
        {
            Id = 2,
            CustomerId = 200,
            OrderItems = []
        };

        processor.Process(validOrder);
        processor.Process(invalidOrder);
    }
}
