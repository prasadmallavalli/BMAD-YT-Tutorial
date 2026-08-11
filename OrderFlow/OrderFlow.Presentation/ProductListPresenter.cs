using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// AD-3: constructor-injected with IView + IServiceScopeFactory — never a long-lived
// IProductService instance. Each action opens its own IServiceScope.
public class ProductListPresenter
{
    private readonly IProductListView _view;
    private readonly IServiceScopeFactory _scopeFactory;

    public ProductListPresenter(IProductListView view, IServiceScopeFactory scopeFactory)
    {
        _view = view;
        _scopeFactory = scopeFactory;
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
}
