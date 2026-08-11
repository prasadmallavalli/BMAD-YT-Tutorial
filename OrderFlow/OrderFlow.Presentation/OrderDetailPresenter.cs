using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation;

// AD-3: constructor-injected with IView + IServiceScopeFactory — never a long-lived
// service instance. Each action opens its own IServiceScope.
public class OrderDetailPresenter
{
    private readonly IOrderDetailView _view;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderDetailPresenter(IOrderDetailView view, IServiceScopeFactory scopeFactory)
    {
        _view = view;
        _scopeFactory = scopeFactory;
    }

    // Returns Task, not Task<bool> like ProductDetailPresenter.LoadAsync — there's nothing
    // conditional to do after loading here (view-only), unlike ProductDetailForm which needs
    // the bool to decide whether to proceed with editing.
    public async Task LoadAsync(int orderId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var result = await orderService.GetAsync(orderId);

        if (!result.IsSuccess)
        {
            _view.ShowError(result.Error!);
            return;
        }

        var order = result.Value!;
        _view.ShowOrder(order);

        // Same scope, same business operation (AD-3) — GetAllowedNextStatuses is a synchronous
        // in-memory lookup, no extra DB round trip.
        var orderStatusService = scope.ServiceProvider.GetRequiredService<IOrderStatusService>();
        var allowedStatuses = orderStatusService.GetAllowedNextStatuses(order.OrderType, order.Status);
        _view.DisplayAvailableTransitions(allowedStatuses);
    }

    // Returns Task<bool>, not void — OrderDetailForm decides whether to call LoadAsync again
    // (Form-orchestrated reload, not chained from inside this method) to keep this a single
    // scoped business operation (AD-5).
    public async Task<bool> TransitionToAsync(int orderId, OrderStatus newStatus)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderStatusService = scope.ServiceProvider.GetRequiredService<IOrderStatusService>();

        var result = await orderStatusService.TransitionTo(orderId, newStatus);

        if (result.IsSuccess)
        {
            return true;
        }

        _view.ShowError(result.Error!);
        return false;
    }
}
