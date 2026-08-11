using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// AD-3: constructor-injected with IView + IServiceScopeFactory — never a long-lived
// IProductService instance. Each action opens its own IServiceScope.
public class ProductDetailPresenter
{
    private readonly IProductDetailView _view;
    private readonly IServiceScopeFactory _scopeFactory;

    public ProductDetailPresenter(IProductDetailView view, IServiceScopeFactory scopeFactory)
    {
        _view = view;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> LoadAsync(int productId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IProductService>();

        var result = await service.GetAsync(productId);

        if (result.IsSuccess)
        {
            _view.ShowProduct(result.Value!);
            return true;
        }

        _view.ShowError(result.Error!);
        return false;
    }

    public async Task<bool> SaveAsync(int? productId, ProductDto dto)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IProductService>();

        var result = productId.HasValue
            ? await service.UpdateAsync(productId.Value, dto)
            : await service.CreateAsync(dto);

        if (result.IsSuccess)
        {
            return true;
        }

        _view.ShowError(result.Error!);
        return false;
    }
}
