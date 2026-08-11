using OrderFlow.Domain;

namespace OrderFlow.Exhibits.Before.Srp;

public static class BeforeSrpRunner
{
    public static void Run()
    {
        Console.WriteLine("=== Before: SRP Violation ===");

        var processor = new OrderProcessor();

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
