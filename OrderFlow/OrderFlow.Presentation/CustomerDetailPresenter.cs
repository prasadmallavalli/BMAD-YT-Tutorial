using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// AD-3: constructor-injected with IView + IServiceScopeFactory — never a long-lived
// ICustomerService instance. Each action opens its own IServiceScope.
public class CustomerDetailPresenter
{
    private readonly ICustomerDetailView _view;
    private readonly IServiceScopeFactory _scopeFactory;

    public CustomerDetailPresenter(ICustomerDetailView view, IServiceScopeFactory scopeFactory)
    {
        _view = view;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> LoadAsync(int customerId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICustomerService>();

        var result = await service.GetAsync(customerId);

        if (result.IsSuccess)
        {
            _view.ShowCustomer(result.Value!);
            return true;
        }

        _view.ShowError(result.Error!);
        return false;
    }

    public async Task<bool> SaveAsync(int? customerId, CustomerDto dto)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICustomerService>();

        var result = customerId.HasValue
            ? await service.UpdateAsync(customerId.Value, dto)
            : await service.CreateAsync(dto);

        if (result.IsSuccess)
        {
            return true;
        }

        _view.ShowError(result.Error!);
        return false;
    }
}
