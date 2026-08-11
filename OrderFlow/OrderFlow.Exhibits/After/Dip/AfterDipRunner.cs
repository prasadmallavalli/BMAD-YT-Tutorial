namespace OrderFlow.Exhibits.After.Dip;

public static class AfterDipRunner
{
    public static void Run()
    {
        Console.WriteLine("=== After: DIP Refactor ===");

        Console.WriteLine("-- with SqlCustomerRepository --");
        var realService = new CustomerLookupService(new SqlCustomerRepository());
        realService.FindCustomer(1);

        Console.WriteLine("-- with FakeCustomerRepository (no real database needed) --");
        var fakeService = new CustomerLookupService(new FakeCustomerRepository());
        fakeService.FindCustomer(1);
        fakeService.FindCustomer(99);
    }
}
