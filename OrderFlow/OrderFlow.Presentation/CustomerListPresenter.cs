using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// AD-3: constructor-injected with IView + IServiceScopeFactory — never a long-lived
// ICustomerService instance. Each action opens its own IServiceScope.
public class CustomerListPresenter
{
    private readonly ICustomerListView _view;
    private readonly IServiceScopeFactory _scopeFactory;

    public CustomerListPresenter(ICustomerListView view, IServiceScopeFactory scopeFactory)
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
}
