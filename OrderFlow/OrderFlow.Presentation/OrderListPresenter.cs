using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// AD-3: constructor-injected with IView + IServiceScopeFactory — never a long-lived
// IOrderService instance. Each action opens its own IServiceScope.
public class OrderListPresenter
{
    private readonly IOrderListView _view;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderListPresenter(IOrderListView view, IServiceScopeFactory scopeFactory)
    {
        _view = view;
        _scopeFactory = scopeFactory;
    }

    public async Task LoadOrdersAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var result = await service.GetAllAsync();

        if (result.IsSuccess)
        {
            _view.DisplayOrders(result.Value!);
        }
        else
        {
            _view.ShowError(result.Error!);
        }
    }
}
