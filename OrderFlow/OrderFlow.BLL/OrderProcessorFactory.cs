using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain;

namespace OrderFlow.BLL;

// AD-7: registered Scoped (never Singleton, despite the name). The constructor-injected
// IServiceProvider is the ambient scoped provider for the current business operation (AD-5) —
// DI supplies this automatically for a Scoped service, never a captured root provider.
// No caller resolves IOrderProcessor directly; Create(OrderType) is the only resolution path.
public class OrderProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public OrderProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Throws InvalidOperationException for an OrderType with no keyed registration (e.g.
    // OrderType.Unspecified) — matches GetRequiredService's established "this should never
    // happen" convention already used throughout Program.cs. Callers (Story 2.5's Presenter)
    // are responsible for only ever passing an OrderType the user actually selected.
    public IOrderProcessor Create(OrderType orderType) =>
        _serviceProvider.GetRequiredKeyedService<IOrderProcessor>(orderType);
}
