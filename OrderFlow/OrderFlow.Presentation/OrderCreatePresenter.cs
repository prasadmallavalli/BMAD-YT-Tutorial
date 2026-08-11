using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// AD-3: constructor-injected with IView + IServiceScopeFactory — never a long-lived
// service instance. Each action opens its own IServiceScope. No IOrderService exists in
// this codebase — OrderProcessorFactory is resolved directly, per its own doc comment.
public class OrderCreatePresenter
{
    private readonly IOrderCreateView _view;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderCreatePresenter(IOrderCreateView view, IServiceScopeFactory scopeFactory)
    {
        _view = view;
        _scopeFactory = scopeFactory;
    }

    public async Task LoadCustomersAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICustomerService>();

        var result = await service.GetAllAsync();

        if (result.IsSuccess)
        {
            _view.DisplayCustomers(result.Value!);
        }
        else
        {
            _view.ShowError(result.Error!);
        }
    }

    public async Task LoadProductsAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IProductService>();

        var result = await service.GetAllAsync();

        if (result.IsSuccess)
        {
            _view.DisplayProducts(result.Value!);
        }
        else
        {
            _view.ShowError(result.Error!);
        }
    }

    public async Task<bool> ConfirmAsync(CreateOrderRequest request)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<OrderProcessorFactory>();
        var processor = factory.Create(request.OrderType);

        var result = await processor.ConfirmAsync(request);

        if (result.IsSuccess)
        {
            return true;
        }

        _view.ShowError(result.Error!);
        return false;
    }
}
