using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation;

internal static class Program
{
    // Sole composition root (AD-1, FR-10). No other type in this solution may call
    // `new ServiceCollection()` / construct an `IServiceProvider`, and no calling code
    // outside here may `new` a BLL/DAL implementation directly.
    [STAThread]
    private static void Main()
    {
        Application.ThreadException += (_, e) => ReportFatal(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ReportFatal(e.ExceptionObject as Exception);

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            using var provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

            ApplicationConfiguration.Initialize();

            var mainForm = provider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            ReportFatal(ex);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // AD-2: AppDbContext/DbSet<T> stay internal to OrderFlow.DAL except for the
        // composition-root exception recorded in AD-1/AD-9. Only a singleton
        // IDbContextFactory<AppDbContext> is registered here; nothing else constructs
        // AppDbContext directly. No DbSets exist yet — see Story 1.2 onward.
        services.AddPooledDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=OrderFlow;Trusted_Connection=True;TrustServerCertificate=True;"));

        // AD-9: IUnitOfWork is Scoped-per-operation; CustomerRepository is constructed
        // internally by UnitOfWork and is never independently DI-registered.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();

        // AD-4/AD-5: INotifier is registered Singleton — UI-side subscribers (Story 3.4) must
        // outlive any single Scoped operation. OrderStatusService stays Scoped-per-operation
        // like every other BLL service.
        services.AddSingleton<INotifier, InAppNotifier>();
        services.AddScoped<IOrderStatusService, OrderStatusService>();

        // AD-11: exactly one IPricingStrategy registered Scoped, no keyed dispatch — swapping
        // strategies later means changing only this one line.
        services.AddScoped<IPricingStrategy, StandardPricingStrategy>();

        // AD-7: IOrderProcessor varies per OrderType via keyed DI (unlike IPricingStrategy's
        // single plain registration above). OrderProcessorFactory is the only resolution path.
        services.AddKeyedScoped<IOrderProcessor, StandardOrderProcessor>(OrderType.Standard);
        services.AddKeyedScoped<IOrderProcessor, RushOrderProcessor>(OrderType.Rush);
        services.AddScoped<OrderProcessorFactory>();

        services.AddTransient<MainForm>();
        services.AddTransient<CustomerListForm>();
        services.AddTransient<CustomerDetailForm>();
        services.AddTransient<ProductListForm>();
        services.AddTransient<ProductDetailForm>();
        services.AddTransient<OrderCreateForm>();
        services.AddTransient<OrderListForm>();
        services.AddTransient<OrderDetailForm>();
    }

    private static void ReportFatal(Exception? ex)
    {
        MessageBox.Show(ex?.ToString() ?? "Unknown fatal error.", "OrderFlow — Fatal Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
