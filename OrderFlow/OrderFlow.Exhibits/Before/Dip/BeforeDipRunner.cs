namespace OrderFlow.Exhibits.Before.Dip;

public static class BeforeDipRunner
{
    public static void Run()
    {
        Console.WriteLine("=== Before: DIP Violation ===");

        var service = new CustomerLookupService();
        service.FindCustomer(1);
        service.FindCustomer(99);
    }
}
